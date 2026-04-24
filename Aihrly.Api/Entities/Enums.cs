namespace Aihrly.Api.Entities;

public enum TeamMemberRole
{
    Recruiter,
    HiringManager
}

public enum JobStatus
{
    Open,
    Closed
}

// terminal states are Hired and Rejected
public enum ApplicationStage
{
    Applied,
    Screening,
    Interview,
    Offer,
    Hired,
    Rejected
}

public enum NoteType
{
    General,
    Screening,
    Interview,
    ReferenceCheck,
    RedFlag
}

public enum ScoreDimension
{
    CultureFit,
    Interview,
    Assessment
}
