namespace TixTapGo.EventService.Enums;

public enum SeatAssignmentType
{
    /// <summary>
    /// Seats are not assigned to attendees
    /// </summary>
    GeneralAdmission = 0,

    /// <summary>
    /// Seats are assigned to attendees
    /// </summary>
    SeatsAssigned = 1,

    //
    Mixed = 2
}
