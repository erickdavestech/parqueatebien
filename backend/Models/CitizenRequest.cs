﻿namespace Models;

public class CitizenRequest : ICitizen
{
    public string LicensePlate { get; set; } = String.Empty;
    public string VehicleType { get; set; } = String.Empty;
    public string VehicleColor { get; set; } = String.Empty;
    public string Address { get; set; } = String.Empty;
    public string Lat { get; set; } = String.Empty;
    public string Lon { get; set; } = String.Empty;
    public List<Pictures> Photos { get; set; } = new List<Pictures>();
}
