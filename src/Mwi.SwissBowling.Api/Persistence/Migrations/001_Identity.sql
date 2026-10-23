CREATE TABLE "Accounts" (
	"Id" uuid PRIMARY KEY, "Issuer" varchar(500) NOT NULL, "Subject" varchar(200) NOT NULL,
	"DisplayName" varchar(200) NOT NULL, "IsApproved" boolean NOT NULL,
	"PlayerId" uuid NULL, "RegisteredAt" timestamptz NOT NULL,
	UNIQUE ("Issuer", "Subject"), UNIQUE ("PlayerId")
);
CREATE TABLE "AuditRecords" (
	"Id" uuid PRIMARY KEY, "Action" text NOT NULL, "Resource" text NOT NULL,
	"Issuer" text NOT NULL, "Subject" text NOT NULL, "OccurredAt" timestamptz NOT NULL, "Details" jsonb NOT NULL
);
CREATE INDEX "IX_AuditRecords_Resource_OccurredAt" ON "AuditRecords" ("Resource", "OccurredAt");
CREATE FUNCTION reject_audit_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN RAISE EXCEPTION 'Audit records are append-only'; END;
$$;
CREATE TRIGGER audit_append_only BEFORE UPDATE OR DELETE ON "AuditRecords" FOR EACH ROW EXECUTE FUNCTION reject_audit_mutation();
