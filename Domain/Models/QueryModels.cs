namespace MyCars.Domain.Models;

public sealed record PageRequest(int Page = 0, int PageSize = 20);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount);

public sealed record VehicleFilter(
    string? VehicleType        = null,
    string? Condition          = null,
    string? Fuel               = null,           // singolo fuel (filtri classici)
    bool?   ProntaConsegna     = null,
    bool?   IsNuovoArrivo      = null,
    decimal? MinPrice          = null,
    decimal? MaxPrice          = null,
    int?    MaxMileageKm       = null,
    int?    MinYear            = null,
    int?    MaxYear            = null,
    int?    MinMonth           = null,   // 1–12
    int?    MaxMonth           = null,   // 1–12
    Guid?   BranchId           = null,
    string? Search             = null,
    string? Transmission       = null,
    bool?   VatDeductible      = null,
    bool?   HandicapAccessible = null,
    bool?   Imported           = null,
    bool?   ForSale            = null,
    bool?   ForRental          = null,
    // Campi aggiuntivi usati dalla ricerca conversazionale AI
    IReadOnlyList<string>? BodyTypes  = null,   // body_type_name ILIKE ANY(...)
    IReadOnlyList<string>? FuelTypes  = null,   // fuel = ANY(...) — multi-valore
    string? Sort                       = null,   // prezzo_asc|prezzo_desc|anno_desc|km_asc
    int?    MinSeats                   = null,   // seats >= MinSeats
    int?    MinHorsepowerCv            = null,   // horsepower_cv >= MinHorsepowerCv
    int?    MaxHorsepowerCv            = null,   // horsepower_cv <= MaxHorsepowerCv
    int?    MinEngineCc                = null,   // engine_capacity_cc >= MinEngineCc
    int?    MaxEngineCc                = null,   // engine_capacity_cc <= MaxEngineCc
    string? Color                      = null,   // color ILIKE %color%
    string? EmissionClass              = null,   // emission_class ILIKE %class%
    string? DescriptionKeyword         = null,   // description ILIKE %keyword%
    bool?   Damaged                    = null,   // damaged = true/false
    string? Brand                      = null,   // brand_name ILIKE %brand% (solo marca)
    string? Model                      = null    // model ILIKE %model% (solo modello)
);
