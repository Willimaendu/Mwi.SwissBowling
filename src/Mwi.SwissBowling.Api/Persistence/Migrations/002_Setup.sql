CREATE TABLE "Players" (
	"Id" uuid PRIMARY KEY, "FirstName" varchar(100) NOT NULL, "LastName" varchar(100) NOT NULL,
	"SfkvNumber" varchar(30) NULL UNIQUE, "DateOfBirth" date NULL, "Email" varchar(254) NULL, "Phone" varchar(50) NULL
);
ALTER TABLE "Accounts" ADD CONSTRAINT "FK_Accounts_Players" FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id");
CREATE TABLE "Clubs" ("Id" uuid PRIMARY KEY, "Name" varchar(160) NOT NULL, "Email" varchar(254) NULL, "Phone" varchar(50) NULL);
CREATE TABLE "Seasons" ("Id" uuid PRIMARY KEY, "Year" integer NOT NULL UNIQUE CHECK ("Year" BETWEEN 1 AND 9999), "RosterLocked" boolean NOT NULL);
CREATE TABLE "SeasonMembers" (
	"SeasonId" uuid NOT NULL REFERENCES "Seasons" ("Id"), "PlayerId" uuid NOT NULL REFERENCES "Players" ("Id"),
	"ClubId" uuid NULL REFERENCES "Clubs" ("Id"), "Category" integer NOT NULL CHECK ("Category" BETWEEN 0 AND 3),
	PRIMARY KEY ("SeasonId", "PlayerId")
);
CREATE INDEX "IX_SeasonMembers_PlayerId" ON "SeasonMembers" ("PlayerId");
CREATE INDEX "IX_SeasonMembers_ClubId" ON "SeasonMembers" ("ClubId");
CREATE TABLE "Venues" ("Id" uuid PRIMARY KEY, "Name" varchar(160) NOT NULL, "Address" varchar(300) NOT NULL);
CREATE TABLE "Lanes" ("VenueId" uuid NOT NULL REFERENCES "Venues" ("Id") ON DELETE CASCADE, "Number" integer NOT NULL CHECK ("Number" > 0), PRIMARY KEY ("VenueId", "Number"));
CREATE TABLE "AnnualChampionships" ("Id" uuid PRIMARY KEY, "SeasonId" uuid NOT NULL REFERENCES "Seasons" ("Id"), "Name" varchar(160) NOT NULL);
CREATE INDEX "IX_AnnualChampionships_SeasonId" ON "AnnualChampionships" ("SeasonId");
CREATE TABLE "Events" (
	"Id" uuid PRIMARY KEY, "AnnualChampionshipId" uuid NOT NULL REFERENCES "AnnualChampionships" ("Id"),
	"Name" varchar(160) NOT NULL, "VenueId" uuid NOT NULL REFERENCES "Venues" ("Id"),
	"StartDate" date NOT NULL, "EndDate" date NOT NULL CHECK ("EndDate" >= "StartDate"),
	"MaximumThrows" integer NULL CHECK ("MaximumThrows" > 0), "State" integer NOT NULL CHECK ("State" BETWEEN 0 AND 2),
	"PublishedRevision" integer NOT NULL CHECK ("PublishedRevision" >= 0)
);
CREATE INDEX "IX_Events_AnnualChampionshipId" ON "Events" ("AnnualChampionshipId");
CREATE INDEX "IX_Events_VenueId" ON "Events" ("VenueId");
