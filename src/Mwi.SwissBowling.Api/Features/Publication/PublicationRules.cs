using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.Api.Features.Publication;

public static class PublicationRules
{
    public static void RequirePublishable(EventState state, int resultCount, bool activeLeases)
    {
        if (state == EventState.Published) throw new RequestRejectedException(409, "Die Meisterschaft ist bereits veröffentlicht.");
        if (resultCount == 0) throw new RequestRejectedException(409, "Mindestens ein gültiges Resultat ist erforderlich.");
        if (activeLeases) throw new RequestRejectedException(409, "Es bestehen aktive Bearbeitungssperren. Schliessen Sie die Erfassung ab oder warten Sie auf deren Ablauf.");
    }
    public static void RequireDifferentApprover(string requesterIssuer, string requesterSubject, string approverIssuer, string approverSubject)
    {
        if (requesterIssuer == approverIssuer && requesterSubject == approverSubject)
            throw new RequestRejectedException(409, "Eine andere administrative Person muss die Wiedereröffnung genehmigen.");
    }
}
