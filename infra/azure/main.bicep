targetScope = 'resourceGroup'

@description('Globally unique lowercase prefix, e.g. jikeda-team3-<suffix>.')
@minLength(4)
@maxLength(20)
param prefix string
param location string = resourceGroup().location
param staticLocation string = 'eastasia'
param deployApps bool = false
@description('Immutable images: identity, catalog, order, fulfilment, gateway. Needed only with deployApps=true.')
param images object = {}
@secure()
param databaseAdminPassword string
@secure()
@description('Distinct passwords for identity, catalog, order, fulfilment runtime roles.')
param databasePasswords object
@secure()
param jwtKey string
param jwtIssuer string = 'JiKeDa.Identity'
param jwtAudience string = 'JiKeDa.Platform'
param catalogHttpConcurrency int = 10
param fulfilmentQueueThreshold int = 5
@minValue(2)
@maxValue(5)
param maxReplicas int = 3

var domains = ['identity', 'catalog', 'order', 'fulfilment']
var registryName = replace(prefix, '-', '')
var runtimePasswordSecrets = [for domain in domains: { name: '${domain}-password', value: databasePasswords[domain] }]
var runtimePasswordEnvironment = [for domain in domains: { name: '${toUpper(domain)}_PASSWORD', secretRef: '${domain}-password' }]

resource network 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: '${prefix}-vnet'
  location: location
  properties: {
    addressSpace: { addressPrefixes: ['10.30.0.0/16'] }
    subnets: [
      {
        name: 'apps'
        properties: {
          addressPrefix: '10.30.0.0/23'
          delegations: [{ name: 'apps', properties: { serviceName: 'Microsoft.App/environments' } }]
        }
      }
      {
        name: 'postgres'
        properties: {
          addressPrefix: '10.30.2.0/24'
          delegations: [{ name: 'postgres', properties: { serviceName: 'Microsoft.DBforPostgreSQL/flexibleServers' } }]
        }
      }
    ]
  }
}
resource dns 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: '${prefix}.postgres.database.azure.com'
  location: 'global'
}
resource dnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: dns
  name: '${prefix}-link'
  location: 'global'
  properties: { registrationEnabled: false, virtualNetwork: { id: network.id } }
}
resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2022-12-01' = {
  name: '${prefix}-pg'
  location: location
  sku: { name: 'Standard_B1ms', tier: 'Burstable' }
  properties: {
    version: '16'
    administratorLogin: 'jikeda_admin'
    administratorLoginPassword: databaseAdminPassword
    storage: { storageSizeGB: 32 }
    backup: { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
    highAvailability: { mode: 'Disabled' }
    network: {
      delegatedSubnetResourceId: '${network.id}/subnets/postgres'
      privateDnsZoneArmResourceId: dns.id
    }
  }
  dependsOn: [dnsLink]
}
resource databases 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2022-12-01' = [for domain in domains: {
  parent: postgres
  name: domain
  properties: { charset: 'UTF8', collation: 'en_US.utf8' }
}]
resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: false }
}
resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}-logs'
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}
resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${prefix}-insights'
  location: location
  kind: 'web'
  properties: { Application_Type: 'web', WorkspaceResourceId: workspace.id }
}
resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${prefix}-apps'
  location: location
  properties: {
    workloadProfiles: [{ name: 'Consumption', workloadProfileType: 'Consumption' }]
    vnetConfiguration: { infrastructureSubnetId: '${network.id}/subnets/apps', internal: false }
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: { customerId: workspace.properties.customerId, sharedKey: workspace.listKeys().primarySharedKey }
    }
  }
}
resource bus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: '${prefix}-bus'
  location: location
  sku: { name: 'Standard', tier: 'Standard' }
  properties: { disableLocalAuth: true, minimumTlsVersion: '1.2' }
}
resource topic 'Microsoft.ServiceBus/namespaces/topics@2021-11-01' = {
  parent: bus
  name: 'domain-events'
  properties: { requiresDuplicateDetection: true, duplicateDetectionHistoryTimeWindow: 'PT10M' }
}
resource subscriptions 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = [for domain in ['order', 'catalog', 'fulfilment']: {
  parent: topic
  name: domain
  properties: { maxDeliveryCount: 5, lockDuration: 'PT1M', deadLetteringOnMessageExpiration: true }
}]
resource filters 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2021-11-01' = [for (domain, i) in ['order', 'catalog', 'fulfilment']: {
  parent: subscriptions[i]
  name: '$Default'
  properties: {
    filterType: 'SqlFilter'
    sqlFilter: { sqlExpression: 'destination = \'${domain}\'' }
  }
}]
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = [for domain in domains: {
  name: '${prefix}-${domain}-mi'
  location: location
}]
resource gatewayIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${prefix}-gateway-mi'
  location: location
}
resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for (domain, i) in domains: {
  name: guid(registry.id, identity[i].id, 'AcrPull')
  scope: registry
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: identity[i].properties.principalId
    principalType: 'ServicePrincipal'
  }
}]
resource gatewayPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, gatewayIdentity.id, 'AcrPull')
  scope: registry
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: gatewayIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}
// One identity per service; receive scope is limited to the service subscription.
resource busSend 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for (domain, i) in ['order', 'catalog', 'fulfilment']: {
  name: guid(topic.id, identity[indexOf(domains, domain)].id, 'sender')
  scope: topic
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39')
    principalId: identity[indexOf(domains, domain)].properties.principalId
    principalType: 'ServicePrincipal'
  }
}]
resource busReceive 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for (domain, i) in ['order', 'catalog', 'fulfilment']: {
  name: guid(subscriptions[i].id, identity[indexOf(domains, domain)].id, 'receiver')
  scope: subscriptions[i]
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4f6e3b88-14fb-41d1-bc54-e29f6080df15')
    principalId: identity[indexOf(domains, domain)].properties.principalId
    principalType: 'ServicePrincipal'
  }
}]
module apps 'service.bicep' = [for (domain, i) in domains: if (deployApps) {
  name: '${domain}-app'
  params: {
    name: '${prefix}-${domain}'
    location: location
    environmentId: environment.id
    image: images[domain]
    registry: registry.properties.loginServer
    identityId: identity[i].id
    clientId: identity[i].properties.clientId
    databaseConnection: 'Host=${postgres.properties.fullyQualifiedDomainName};Database=${domain};Username=${domain}_app;Password=${databasePasswords[domain]};SSL Mode=Require'
    jwtKey: jwtKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    insightsConnection: insights.properties.ConnectionString
    busNamespace: '${bus.name}.servicebus.windows.net'
    domain: domain
    httpConcurrency: domain == 'catalog' ? catalogHttpConcurrency : 10
    queueThreshold: fulfilmentQueueThreshold
    maxReplicas: maxReplicas
    upstreamPrefix: prefix
    upstreamDomain: environment.properties.defaultDomain
  }
  dependsOn: [acrPull, busSend, busReceive, databases]
}]
module gateway 'service.bicep' = if (deployApps) {
  name: 'gateway-app'
  params: {
    name: '${prefix}-gateway'
    location: location
    environmentId: environment.id
    image: images.gateway
    registry: registry.properties.loginServer
    identityId: gatewayIdentity.id
    clientId: gatewayIdentity.properties.clientId
    external: true
    databaseConnection: ''
    jwtKey: jwtKey
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    insightsConnection: insights.properties.ConnectionString
    busNamespace: ''
    domain: 'gateway'
    maxReplicas: maxReplicas
    upstreamPrefix: prefix
    upstreamDomain: environment.properties.defaultDomain
  }
  dependsOn: [apps, gatewayPull]
}
resource web 'Microsoft.Web/staticSites@2023-12-01' = [for suffix in ['web', 'display']: {
  name: '${prefix}-${suffix}'
  location: staticLocation
  sku: { name: 'Free', tier: 'Free' }
  properties: { allowConfigFileUpdates: true }
}]
output environmentName string = environment.name
output registryServer string = registry.properties.loginServer
output postgresHost string = postgres.properties.fullyQualifiedDomainName
output busNamespace string = bus.name
output webHosts array = [for (suffix, i) in ['web', 'display']: web[i].properties.defaultHostname]

resource bootstrap 'Microsoft.App/jobs@2024-03-01' = {
  name: '${prefix}-db-bootstrap'
  location: location
  properties: {
    environmentId: environment.id
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 300
      replicaRetryLimit: 0
      manualTriggerConfig: { parallelism: 1, replicaCompletionCount: 1 }
      secrets: concat([{ name: 'admin-password', value: databaseAdminPassword }], runtimePasswordSecrets)
    }
    template: {
      containers: [{
        name: 'bootstrap'
        image: 'postgres:16-alpine'
        command: ['sh', '-ec', loadTextContent('bootstrap.sh')]
        resources: { cpu: json('0.25'), memory: '0.5Gi' }
        env: concat([
          { name: 'PGHOST', value: postgres.properties.fullyQualifiedDomainName }
          { name: 'PGUSER', value: 'jikeda_admin' }
          { name: 'PGDATABASE', value: 'postgres' }
          { name: 'PGSSLMODE', value: 'require' }
          { name: 'PGPASSWORD', secretRef: 'admin-password' }
        ], runtimePasswordEnvironment)
      }]
    }
  }
  dependsOn: [databases]
}
output bootstrapJobName string = bootstrap.name
