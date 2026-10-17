param name string
param location string
param environmentId string
param image string
param registry string
param identityId string
param clientId string
param domain string
param external bool = false
param upstreamPrefix string = ''
param upstreamDomain string = ''
param httpConcurrency int = 10
param queueThreshold int = 5
param maxReplicas int = 3
@secure()
param databaseConnection string
@secure()
param jwtKey string
param jwtIssuer string
param jwtAudience string
@secure()
param insightsConnection string
param busNamespace string

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identityId}': {} } }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: external, targetPort: 8080, transport: 'http', allowInsecure: false }
      registries: [{ server: registry, identity: identityId }]
      secrets: [
        { name: 'database', value: databaseConnection }
        { name: 'jwt', value: jwtKey }
        { name: 'insights', value: insightsConnection }
      ]
    }
    template: {
      containers: [{
        name: domain
        image: image
        resources: { cpu: json('0.5'), memory: '1Gi' }
        env: concat([
          { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
          { name: 'ASPNETCORE_ENVIRONMENT', value: 'Staging' }
          { name: 'ConnectionStrings__Default', secretRef: 'database' }
          { name: 'Jwt__Key', secretRef: 'jwt' }
          { name: 'Jwt__Issuer', value: jwtIssuer }
          { name: 'Jwt__Audience', value: jwtAudience }
          { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', secretRef: 'insights' }
          { name: 'AZURE_CLIENT_ID', value: clientId }
          { name: 'ServiceBus__FullyQualifiedNamespace', value: busNamespace }
          { name: 'ServiceBus__Topic', value: 'domain-events' }
          { name: 'ServiceBus__Subscription', value: domain }
          { name: 'Database__MigrateOnStartup', value: 'false' }
        ], [
          { name: 'Services__Identity', value: 'https://${upstreamPrefix}-identity.internal.${upstreamDomain}' }
          { name: 'Services__Catalog', value: 'https://${upstreamPrefix}-catalog.internal.${upstreamDomain}' }
          { name: 'Services__Order', value: 'https://${upstreamPrefix}-order.internal.${upstreamDomain}' }
          { name: 'Services__Fulfilment', value: 'https://${upstreamPrefix}-fulfilment.internal.${upstreamDomain}' }
        ])
        probes: [
          { type: 'Startup', httpGet: { path: '/health/live', port: 8080 }, periodSeconds: 5, failureThreshold: 30 }
          { type: 'Liveness', httpGet: { path: '/health/live', port: 8080 }, periodSeconds: 10, failureThreshold: 3 }
          { type: 'Readiness', httpGet: { path: '/health/ready', port: 8080 }, periodSeconds: 5, failureThreshold: 3 }
        ]
      }]
      scale: {
        minReplicas: 1
        maxReplicas: maxReplicas
        rules: concat([
          { name: 'http', http: { metadata: { concurrentRequests: string(httpConcurrency) } } }
        ], domain == 'fulfilment' ? [
          {
            name: 'servicebus-backlog'
            custom: {
              type: 'azure-servicebus'
              identity: identityId
              metadata: { namespace: first(split(busNamespace, '.')), topicName: 'domain-events', subscriptionName: domain, messageCount: string(queueThreshold) }
            }
          }
        ] : [])
      }
    }
  }
}
output name string = app.name
output fqdn string = app.properties.configuration.ingress.fqdn
