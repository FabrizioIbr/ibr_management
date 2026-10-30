using System.Text.Json.Serialization;

namespace Ibr.Infrastructure.Buk;

internal sealed class BukPage<T>
{
    [JsonPropertyName("pagination")]
    public BukPagination? Pagination { get; set; }

    [JsonPropertyName("data")]
    public List<T>? Data { get; set; }
}

internal sealed class BukPagination
{
    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("previous")]
    public string? Previous { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }
}

internal sealed class BukArea
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("parent_area")]
    public BukAreaParent? ParentArea { get; set; }

    [JsonPropertyName("first_level_id")]
    public int? FirstLevelId { get; set; }

    [JsonPropertyName("first_level_name")]
    public string? FirstLevelName { get; set; }

    [JsonPropertyName("second_level_id")]
    public int? SecondLevelId { get; set; }

    [JsonPropertyName("second_level_name")]
    public string? SecondLevelName { get; set; }

    [JsonPropertyName("cost_center")]
    public string? CostCenter { get; set; }
}

internal sealed class BukAreaParent
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }
}

internal sealed class BukCargo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("requirements")]
    public string? Requirements { get; set; }

    [JsonPropertyName("role_family")]
    public BukRoleFamily? RoleFamily { get; set; }

    [JsonPropertyName("area_ids")]
    public List<int>? AreaIds { get; set; }
}

internal sealed class BukRoleFamily
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal sealed class BukEmployee
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("person_id")]
    public int? PersonId { get; set; }

    [JsonPropertyName("code_sheet")]
    public string? CodeSheet { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("surname")]
    public string? Surname { get; set; }

    [JsonPropertyName("second_surname")]
    public string? SecondSurname { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("personal_email")]
    public string? PersonalEmail { get; set; }

    [JsonPropertyName("active_since")]
    public string? ActiveSince { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("active_until")]
    public string? ActiveUntil { get; set; }

    [JsonPropertyName("termination_reason")]
    public string? TerminationReason { get; set; }

    [JsonPropertyName("progressive_vacations_start")]
    public string? ProgressiveVacationsStart { get; set; }

    [JsonPropertyName("current_job")]
    public BukEmployeeJob? CurrentJob { get; set; }
}

internal sealed class BukEmployeeResponse
{
    [JsonPropertyName("data")]
    public BukEmployee? Data { get; set; }
}

internal sealed class BukEarnedVacation
{
    [JsonPropertyName("period_start_date")]
    public string? PeriodStartDate { get; set; }

    [JsonPropertyName("earned_days")]
    public decimal EarnedDays { get; set; }

    [JsonPropertyName("proportional")]
    public bool? Proportional { get; set; }

    [JsonPropertyName("expiration_date")]
    public string? ExpirationDate { get; set; }
}

internal sealed class BukVacationRequest
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("employee_id")]
    public int EmployeeId { get; set; }

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }

    [JsonPropertyName("calendar_days")]
    public decimal CalendarDays { get; set; }

    [JsonPropertyName("working_days")]
    public decimal WorkingDays { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("vacation_type_id")]
    public int? VacationTypeId { get; set; }

    [JsonPropertyName("requested_at")]
    public DateTime? RequestedAt { get; set; }

    [JsonPropertyName("approved_at")]
    public DateTime? ApprovedAt { get; set; }
}

internal sealed class BukEmployeeJob
{
    [JsonPropertyName("company_id")]
    public int? CompanyId { get; set; }

    [JsonPropertyName("area_id")]
    public int? AreaId { get; set; }

    [JsonPropertyName("main_area_id")]
    public int? MainAreaId { get; set; }

    [JsonPropertyName("role")]
    public BukEmployeeRole? Role { get; set; }

    [JsonPropertyName("boss")]
    public BukEmployeeBoss? Boss { get; set; }

    [JsonPropertyName("contract_type")]
    public string? ContractType { get; set; }

    [JsonPropertyName("employment_regimen")]
    public string? EmploymentRegimen { get; set; }

    [JsonPropertyName("regular_hours")]
    public decimal? RegularHours { get; set; }

    [JsonPropertyName("days")]
    public List<string>? Days { get; set; }
}

internal sealed class BukEmployeeRole
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }
}

internal sealed class BukEmployeeBoss
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }
}
