﻿namespace Models;

public class CitizenRequest
{
    public string LicensePlate { get; set; } = String.Empty;
    public string VehicleType { get; set; } = String.Empty;
    public string VehicleColor { get; set; } = String.Empty;
    public string Address { get; set; } = String.Empty;
    public string Lat { get; set; } = String.Empty;
    public string Lon { get; set; } = String.Empty;
    public List<PicturesRequest> Photos { get; set; } = new List<PicturesRequest>();
}
/*
 {
    "licensePlate": string,
    "vehicleType": string,
    "vehicleColor": string,
    "address": string,
    "lat": string,
    "lon": string,
    "photos": [
        {
            "fileType": string,
            "file": base64
        }
    ]
}
*/