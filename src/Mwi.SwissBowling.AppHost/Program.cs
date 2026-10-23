using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);
var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var identityPassword = builder.AddParameter("identity-password", secret: true);
var identityUser = builder.AddParameter("identity-user", "keycloak");
var keycloakPassword = builder.AddParameter("keycloak-password", secret: true);
var developmentPassword = builder.AddParameter("development-password", secret: true);
var webSecret = builder.AddParameter("web-client-secret", secret: true);
var certificatePath = builder.AddParameter("certificate-path");
var certificatePassword = builder.AddParameter("certificate-password", secret: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword).WithImageTag("18").WithDataVolume();
var application = postgres.AddDatabase("application", "swissbowling");
var webState = postgres.AddDatabase("webstate", "swissbowling_web");
var identityServer = builder.AddPostgres("identitydb", identityUser, identityPassword).WithImageTag("18").WithDataVolume();
var identityDatabase = identityServer.AddDatabase("keycloakdb", "keycloak");
var mail = builder.AddContainer("mail", "axllent/mailpit", "v1.31.6")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "http")
    .WithEndpoint(targetPort: 1025, name: "smtp");
var identityEndpoint = identityServer.GetEndpoint("tcp");
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.8.1")
    .WithArgs("start-dev", "--import-realm")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithBindMount("../../infra/keycloak", "/opt/keycloak/data/import", isReadOnly: true)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "bootstrap")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakPassword)
    .WithEnvironment("KC_HOSTNAME", "http://localhost:8080")
    .WithEnvironment("SWISSBOWLING_DEV_PASSWORD", developmentPassword)
    .WithEnvironment("SWISSBOWLING_WEB_SECRET", webSecret)
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_USERNAME", identityUser)
    .WithEnvironment("KC_DB_PASSWORD", identityPassword)
    .WithEnvironment("KC_DB_URL", ReferenceExpression.Create($"jdbc:postgresql://{identityEndpoint.Property(EndpointProperty.Host)}:{identityEndpoint.Property(EndpointProperty.Port)}/keycloak"))
    .WithEnvironment("MAIL_HOST", mail.GetEndpoint("smtp").Property(EndpointProperty.Host))
    .WithEnvironment("MAIL_PORT", mail.GetEndpoint("smtp").Property(EndpointProperty.Port))
    .WaitFor(identityDatabase)
    .WaitFor(mail)
    .WithHttpHealthCheck("/realms/swissbowling/.well-known/openid-configuration");

var apiMigration = builder.AddProject<Projects.Mwi_SwissBowling_Api>("migrate-api", options => options.ExcludeLaunchProfile = true)
    .WithArgs("--migrate")
    .WithReference(application)
    .WaitFor(application);
var webMigration = builder.AddProject<Projects.Mwi_SwissBowling_Web>("migrate-web", options => options.ExcludeLaunchProfile = true)
    .WithArgs("--migrate")
    .WithReference(webState)
    .WaitFor(webState);

var api = builder.AddProject<Projects.Mwi_SwissBowling_Api>("api")
    .WithReference(application)
    .WithEnvironment("Authentication__Authority", "http://localhost:8080/realms/swissbowling")
    .WithEnvironment("Authentication__Audience", "swissbowling-api")
    .WaitForCompletion(apiMigration)
    .WaitFor(application)
    .WaitFor(keycloak);

builder.AddProject<Projects.Mwi_SwissBowling_Web>("web")
    .WithReference(webState)
    .WithReference(api)
    .WithEnvironment("Authentication__Authority", "http://localhost:8080/realms/swissbowling")
    .WithEnvironment("Authentication__ClientId", "swissbowling-web")
    .WithEnvironment("Authentication__ClientSecret", webSecret)
    .WithEnvironment("DataProtection__CertificatePath", certificatePath)
    .WithEnvironment("DataProtection__CertificatePassword", certificatePassword)
    .WaitForCompletion(webMigration)
    .WaitFor(webState)
    .WaitFor(api);

await builder.Build().RunAsync();
