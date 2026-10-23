CREATE TABLE "PublishedSnapshots" (
	"EventId" uuid NOT NULL REFERENCES "Events" ("Id"), "Revision" integer NOT NULL CHECK ("Revision" > 0),
	"Name" varchar(160) NOT NULL, "Season" integer NOT NULL, "PublishedAt" timestamptz NOT NULL, "Payload" jsonb NOT NULL,
	PRIMARY KEY ("EventId", "Revision")
);
CREATE FUNCTION reject_snapshot_mutation() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN RAISE EXCEPTION 'Published snapshots are immutable'; END;
$$;
CREATE TRIGGER snapshot_immutable BEFORE UPDATE OR DELETE ON "PublishedSnapshots" FOR EACH ROW EXECUTE FUNCTION reject_snapshot_mutation();
CREATE TABLE "CorrectionRequests" (
	"Id" uuid PRIMARY KEY, "EventId" uuid NOT NULL REFERENCES "Events" ("Id"), "Reason" varchar(1000) NOT NULL,
	"RequestedIssuer" varchar(500) NOT NULL, "RequestedSubject" varchar(200) NOT NULL, "RequestedAt" timestamptz NOT NULL,
	"ApprovedAt" timestamptz NULL, "ApprovedIssuer" varchar(500) NULL, "ApprovedSubject" varchar(200) NULL
);
CREATE UNIQUE INDEX "IX_CorrectionRequests_EventId_Pending" ON "CorrectionRequests" ("EventId") WHERE "ApprovedAt" IS NULL;
