using Newtonsoft.Json;
using db;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.ComponentModel;
using Models;

namespace Services;

public struct ValidationResult<T>
{
    public T? Result { get; set; }
    public List<String> ErrorMessages { get; set; }
}

public class CitizensService
{
    public DbConnection connectiondb = new DbConnection();

    public ValidationResult<string> ValidateCitizenRequest(string licensePlate)
    {
        if (licensePlate == null)
        {
            return new ValidationResult<string>() { Result = null, ErrorMessages = new List<string>() { "Route parameter 'licensePlate' was not provided." } };
        }
        if (licensePlate!.Trim().Length >= 5 && licensePlate!.Trim().Length <= 7 && Regex.IsMatch(licensePlate, "^[A-Z0-9-]*$"))
        {
            return new ValidationResult<string>() { Result = licensePlate, ErrorMessages = new() };
        }
        else
        {
            return new ValidationResult<string>() { Result = null, ErrorMessages = new List<string>() { $"License plate '{licensePlate}' is invalid." } };
        }
    }
    public bool ValidateCitizenBody(CitizenRequest citizen)
    {
        return (!string.IsNullOrEmpty(citizen!.LicensePlate) && citizen.LicensePlate is string) &&
        (!string.IsNullOrEmpty(citizen.VehicleType) && citizen.VehicleType is string) &&
        (!string.IsNullOrEmpty(citizen.Lat) && citizen.Lat is string) &&
        (!string.IsNullOrEmpty(citizen.Lon) && citizen.Lon is string);
    }
    public Citizen? UpdateCitizen(Citizen citizen)
    {
        return connectiondb.UpdateCitizen(citizen);
    }
}