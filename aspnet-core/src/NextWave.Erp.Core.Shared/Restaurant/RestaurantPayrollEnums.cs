namespace NextWave.Erp.Restaurant;

public enum RestaurantEmploymentType
{
    Monthly = 0,
    Hourly = 1
}

public enum RestaurantAttendanceStatus
{
    Present = 0,
    Late = 1,
    Leave = 2,
    Absent = 3
}

public enum RestaurantPayrollRunStatus
{
    Draft = 0,
    Approved = 1,
    Paid = 2
}

public enum RestaurantEmployeeLoginMode
{
    None = 0,
    LinkExisting = 1,
    CreateNew = 2
}
