namespace MyCars.Domain.Models;

// DTO compilato dall'LLM tramite function calling; mai generato da SQL diretto.
public sealed class SearchCriteria
{
    // berlina | suv | station_wagon | city_car | monovolume | coupe | cabrio | pickup
    public List<string>? BodyType     { get; set; }

    // benzina | diesel | gpl | metano | ibrida | elettrica
    public List<string>? FuelType     { get; set; }

    public decimal? PriceMax     { get; set; }
    public decimal? PriceMin     { get; set; }

    // "famiglia con due bambini" => almeno 5
    public int?     MinSeats     { get; set; }

    // manuale | automatico
    public string?  Transmission { get; set; }

    // acquisto | noleggio | qualsiasi  (required dal modello)
    public string   Intent       { get; set; } = "qualsiasi";

    // prezzo_asc | prezzo_desc | anno_desc | km_asc | rilevanza  (required dal modello)
    public string   Sort         { get; set; } = "rilevanza";
}
