CREATE TABLE "Results" (
	"EventId" uuid NOT NULL REFERENCES "Events" ("Id"), "PlayerId" uuid NOT NULL, "SeasonId" uuid NOT NULL,
	"Total" integer NOT NULL CHECK ("Total" >= 0), "Nines" integer NOT NULL CHECK ("Nines" >= 0),
	"Eights" integer NOT NULL CHECK ("Eights" >= 0), "Interrupted" boolean NOT NULL,
	"Version" bigint NOT NULL CHECK ("Version" > 0), PRIMARY KEY ("EventId", "PlayerId"),
	FOREIGN KEY ("SeasonId", "PlayerId") REFERENCES "SeasonMembers" ("SeasonId", "PlayerId"),
	CHECK (9::bigint * "Nines" + 8::bigint * "Eights" <= "Total")
);
CREATE INDEX "IX_Results_SeasonId_PlayerId" ON "Results" ("SeasonId", "PlayerId");
CREATE TABLE "EditingLeases" (
	"EventId" uuid NOT NULL REFERENCES "Events" ("Id") ON DELETE CASCADE,
	"PlayerId" uuid NOT NULL REFERENCES "Players" ("Id"), "Token" uuid NOT NULL,
	"Issuer" varchar(500) NOT NULL, "Subject" varchar(200) NOT NULL, "ExpiresAt" timestamptz NOT NULL,
	PRIMARY KEY ("EventId", "PlayerId")
);
CREATE INDEX "IX_EditingLeases_ExpiresAt" ON "EditingLeases" ("ExpiresAt");
CREATE INDEX "IX_EditingLeases_PlayerId" ON "EditingLeases" ("PlayerId");
