namespace MyCars.Domain.Models;

public sealed record PageRequest(int Page = 0, int PageSize = 20);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount);

public sealed record VehicleFilter(
    string?  VehicleType        = null,
    string?  Condition          = null,
    string?  Fuel               = null,
    bool?    ProntaConsegna     = null,
    bool?    IsNuovoArrivo      = null,
    decimal? MinPrice           = null,
    decimal? MaxPrice           = null,
    int?     MaxMileageKm       = null,
    int?     MinYear            = null,
    int?     MaxYear            = null,
    int?     MinMonth           = null,
    int?     MaxMonth           = null,
    Guid?    BranchId           = null,
    string?  Search             = null,
    string?  Transmission       = null,
    bool?    VatDeductible      = null,
    bool?    HandicapAccessible = null,
    bool?    Imported           = null,
    bool?    ForSale            = null,
    bool?    ForRental          = null
);
