-- NOTIFY is delivered only on commit. Messages contain no domain data.
CREATE FUNCTION notify_administration_changed() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
	PERFORM pg_notify('swissbowling_changes', 'administration');
	RETURN NULL;
END;
$$;

DO $$
DECLARE table_name text;
BEGIN
	FOREACH table_name IN ARRAY ARRAY['Accounts', 'Players', 'Clubs', 'Seasons', 'SeasonMembers', 'Venues', 'Lanes', 'AnnualChampionships', 'Events', 'Results', 'CorrectionRequests']
	LOOP
		EXECUTE format('CREATE TRIGGER administration_changed AFTER INSERT OR UPDATE OR DELETE ON %I FOR EACH STATEMENT EXECUTE FUNCTION notify_administration_changed()', table_name);
	END LOOP;
END;
$$;

CREATE FUNCTION notify_publication_changed() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
	PERFORM pg_notify('swissbowling_changes', 'public');
	RETURN NULL;
END;
$$;
CREATE TRIGGER publication_changed AFTER INSERT ON "PublishedSnapshots"
	FOR EACH STATEMENT EXECUTE FUNCTION notify_publication_changed();
CREATE TRIGGER published_event_state_changed AFTER UPDATE OF "State" ON "Events"
	FOR EACH ROW WHEN (NEW."PublishedRevision" > 0 AND OLD."State" IS DISTINCT FROM NEW."State")
	EXECUTE FUNCTION notify_publication_changed();
