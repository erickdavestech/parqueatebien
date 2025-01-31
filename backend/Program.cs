using System.Text.Json;
using backend.Models;
using backend.Interfaces;
using backend.DTOs;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using backend.Services;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using AutoMapper;
using FluentValidation;
using backend.Validations;
using System.Text.RegularExpressions;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
// builder.Logging.ClearProviders();
// builder.Logging.AddConsole();
// builder.Logging.AddDebug();
// builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("http://127.0.0.1:5501", "http://127.0.0.1:50775", "http://localhost:3000","http://192.168.100.10:8089")
                  .AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Firabase configuration
// FirebaseApp.Create(new AppOptions()
// {
//     Credential = GoogleCredential.FromFile(/*Path.Combine(AppDomain.CurrentDomain.BaseDirectory,*/"parqueatebien-484af-firebase-adminsdk-zi7ox-f119c9e3f7.json")/*)*/// MISSING VERY IMPORTANT <- PATH TO SERVICE ACCOUNT FILE.json
// });

// token: BHxgcdvTWWPgRmijvMQx-GC8w9cfP5wJHNXyJX1eq7u6_8Gb5PZSac4gS2F-qYIFkwquJVPscGFx0RdHVdmMWt0

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Prevent object cycles
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    // No necesitas ajustar MaxDepth porque los ciclos serán ignorados
});

// Register Notification Service
builder.Services.AddSingleton<NotificationService>();

// Database connection EF
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

//Token Generator
builder.Services.AddScoped<TokenService>();

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Authorization
builder.Services.AddAuthorization();

builder.Services.AddControllers();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingConfig));

// Validation
builder.Services.AddScoped<IValidator<ReportDto>, ReportDtoValidator>();
builder.Services.AddScoped<IValidator<UserDto>, UserDtoValidator>();
builder.Services.AddScoped<IValidator<CitizenDto>, CitizenDtoValidator>();
builder.Services.AddScoped<IValidator<CitizenVehicle>, CitizenVehicleValidator>();

var app = builder.Build();

// Aplicar migraciones y sembrar la base de datos al arrancar la aplicación
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();
        ApplicationDbContext.Seed(db);
    }
    catch(Exception ex)
    {
        // Registrar el error
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrio un error aplicando las migraciones a la base de datos");
    }
}

// Middleware para captura de errores
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints for citizens

app.MapGet("/", () =>
{
    return Results.Ok("Nothing available here");
});

// Returns a list of all reports in the database
app.MapGet("/api/reportes", async (IMapper _mapper, ApplicationDbContext context) =>
{
    try
    {
        var reportsQuery = context.Reports
        .Where(r => r.Active)
        .Select(report => new
        {
            Report = report,
            Pictures = context.Pictures.Where(p => p.LicensePlate == report.LicensePlate).ToList()
        });

        var reportsData = await reportsQuery.ToListAsync();

        List<ReportResponseDto> reports = new List<ReportResponseDto>();
        foreach (var reportData in reportsData)
        {
            ReportResponseDto reportDto = _mapper.Map<ReportResponseDto>(reportData.Report);
            reportDto.Photos = _mapper.Map<List<PictureDto>>(reportData.Pictures);
            reports.Add(reportDto);
        }

        return Results.Ok(reports);
    }
    catch(Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
    
})
    .WithName("GetAllReports")
    .Produces<List<ReportResponseDto>>(200)
    .RequireAuthorization();

// Returns a single report by licenseplate
app.MapGet("/api/reporte/{licensePlate}", async (IMapper _mapper, ApplicationDbContext context,[FromRoute] string licensePlate) =>
{
    try
    {
        var reportData = await context.Reports
        .OrderByDescending(r => r.Id)
        .Where(r => r.LicensePlate == licensePlate)
        .Select(report => new
        {
            Report = report,
            Pictures = context.Pictures.Where(p => p.LicensePlate == report.LicensePlate).ToList()
        })
        .FirstOrDefaultAsync();

        if (reportData == null)
        {
            return Results.NotFound();
        }

        ReportResponseDto reportDto = _mapper.Map<ReportResponseDto>(reportData.Report);
        reportDto.Photos = _mapper.Map<List<PictureDto>>(reportData.Pictures);

        return Results.Ok(reportDto);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .WithName("GetReport")
    .Produces<ReportResponseDto>(200);

// Add new report to the database, receives ReportDto
app.MapPost("/api/reporte", async (
    IValidator<ReportDto> validator, IMapper _mapper,
    ApplicationDbContext context, [FromBody] ReportDto reportDto,
    NotificationService notificationService) =>
{
    try
    {
        var validationResult = await validator.ValidateAsync(reportDto);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }
        var existingReports = await context.Reports
            .Where(r => r.LicensePlate == reportDto.LicensePlate && r.Status != "Liberado" && r.Active)
            .AnyAsync();

        if (existingReports)
        {
            return Results.Conflict("Ya existe un reporte activo para esta placa que no está liberado.");
        }
        var report = _mapper.Map<Report>(reportDto);
        report.ReportedDate = DateTime.Now.ToString("g");
        var lastReport = await context.Reports.OrderByDescending(r => r.Id).FirstOrDefaultAsync();
        int id = lastReport != null ? lastReport.Id + 1 : 1;
        report.RegistrationNumber = $"PB-{DateTime.Now.Year}-{DateTime.Now.Month}{DateTime.Now.Day}-{id}";
        var photos = _mapper.Map<List<Picture>>(reportDto.Photos);
        foreach (var photo in photos)
        {
            photo.LicensePlate = report.LicensePlate!;
            context.Pictures.Add(photo);
        }
        report.Photos = photos;
        context.Reports.Add(report);

        var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == reportDto.LicensePlate);

        if(vehicle != null)
        {
            var citizen = await context.Citizens.FirstOrDefaultAsync(c => c.GovernmentId == vehicle.GovernmentId);
            if (citizen != null && !String.IsNullOrEmpty(citizen.NotificationToken))
                await notificationService.SendNotificationAsync(citizen.NotificationToken,"Reporte del vehículo",$"Tu vehículo con placa {reportDto.LicensePlate} ha sido reportado.");
        }

        await context.SaveChangesAsync();

        return Results.Created("/api/report/" + reportDto.LicensePlate,
            _mapper.Map<ReportResponseDto>(context.Reports.FirstOrDefault(r => r.LicensePlate == report.LicensePlate)));
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Delete last report from the database by license plate
app.MapDelete("/api/reporte/{licensePlate}", async (ApplicationDbContext context, [FromRoute] string licensePlate) =>
{
    try
    {
        if (licensePlate == null || !Regex.IsMatch(licensePlate, @"^[A-Z]{1,2}[0-9]{6}$"))
        {
            return Results.BadRequest("La placa no es valida");
        }
        var report = await context.Reports.FirstOrDefaultAsync(r => r.LicensePlate == licensePlate);
        if (report == null)
        {
            return Results.NotFound();
        }
        var pictures = await context.Pictures.Where(p => p.LicensePlate == licensePlate).ToListAsync();
        context.Pictures.RemoveRange(pictures);
        context.Reports.Remove(report);
        await context.SaveChangesAsync();
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Update the status of the report, statuses could be "Incautado por grua", "Retenido", "Liberado"
app.MapPut("/api/reporte/actualizarEstado", async (ApplicationDbContext context, [FromBody] ChangeStatusDTO changeStatusDTO, ClaimsPrincipal user) =>
{
    try
    {
        if (!changeStatusDTO.Validate())
        {
            return Results.BadRequest("Esta informacion no es valida");
        }
        var report = context.Reports.Where(r => r.LicensePlate == changeStatusDTO.LicensePlate 
                                    && (r.Status != "Liberado" && r.Active))
                                    .ToList().FirstOrDefault();
        if(report == null)
        {
            return Results.NotFound();
        }
        else
        {
            if (changeStatusDTO.NewStatus == "Incautado por grua" && report.Status == "Reportado")
            {
                report.Status = changeStatusDTO.NewStatus;
                report.TowedByCraneDate = DateTime.Now.ToString("g");
            }

            else if (changeStatusDTO.NewStatus == "Retenido" && report.Status == "Incautado por grua")
            {
                report.Status = "Retenido";
                report.ArrivalAtParkinglot = DateTime.Now.ToString("g");
            }
            else if (changeStatusDTO.NewStatus == "Liberado" && report.Status == "Retenido")
            {
                report.Status = "Liberado";
                report.ReleasedBy = changeStatusDTO.Username;
                report.ReleaseDate = DateTime.Now.ToString("g");
            }
            else
            {
                return Results.Conflict("No puede ser modificado a ese estado");
            }
            await context.SaveChangesAsync();
            return Results.Ok("El estatus fue actualizado");
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Cancel report
app.MapPut("/api/reporte/cancelar/{licensePlate}", async (ApplicationDbContext context, [FromRoute] string licensePlate) =>
{
    try
    {
        var report = context.Reports.FirstOrDefault(r => r.LicensePlate == licensePlate);
        if (report == null)
            return Results.NotFound();

        if (!report.Active)
            return Results.Conflict(new { Message = "Already canceled" });

        report.Active = false;
        report.Status = "Cancelado";
        await context.SaveChangesAsync();
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Returns the amount of reports by status
app.MapGet("/api/reportes/estadisticas", (ApplicationDbContext context) =>
{
    int reportados = context.Reports.Where(r => r.Status == "Reportado").ToList().Count;
    int incautados = context.Reports.Where(r => r.Status == "Incautado por grua").ToList().Count;
    int retenidos = context.Reports.Where(r => r.Status == "Retenido").ToList().Count;
    int liberados = context.Reports.Where(r => r.Status == "Liberado").ToList().Count;

    return Results.Ok(new { reportados, incautados, retenidos, liberados });
})
    .RequireAuthorization();

// USUARIOS

// Register user
app.MapPost("/api/user/register", async (IValidator<UserDto> validator, IMapper _mapper,
    UserDto userDto, ApplicationDbContext context) =>
{
    try
    {
        var validationResult = await validator.ValidateAsync(userDto);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }
        bool existUser = context.Users.FirstOrDefault(u => u.EmployeeCode == userDto.EmployeeCode || u.Username == userDto.Username)
                    != null ? true : false;

        if (existUser)
        {
            return Results.Conflict("Este usuario ya existe existe");
        }
        if (!String.IsNullOrEmpty(userDto.CraneCompany) && userDto.Role == "Grua")
        {
            var craneCompany = context.CraneCompanies.FirstOrDefault(c => c.CompanyName == userDto.CraneCompany);
            if (craneCompany == null)
            {
                return Results.Conflict("Compañia de grua no existe");
            }
            var user = _mapper.Map<User>(userDto);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
            context.Users.Add(user);
            craneCompany.AmountCraneAgents += 1;
            await context.SaveChangesAsync();
            return Results.Created("/api/user/" + user.EmployeeCode, user);
        }
        else if ((userDto.Role == "Admin" || userDto.Role == "Agente") && String.IsNullOrEmpty(userDto.CraneCompany))
        {
            var user = _mapper.Map<User>(userDto);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            return Results.Ok(new { Message = "User registered successfully" });
        }
        else
        {
            return Results.BadRequest("Rol no es valido");
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
    
});

// Login user
app.MapPost("/api/user/login", ([FromBody] UserLoginDto userDto, ApplicationDbContext context, TokenService tokenService) =>
{
    try
    {
        var user = context.Users.FirstOrDefault(u => u.Username == userDto.Username);

        if( user == null)
        {
            return Results.NotFound("No se encontro el usuario.");
        }

        if (!BCrypt.Net.BCrypt.Verify(userDto.Password, user!.PasswordHash))
        {
            return Results.Conflict(new { Message = "Usuario y/o contraseña incorrectos" });
        }
        var token = tokenService.GenerateToken(user);
        return Results.Ok(new {Token=token, Role=user.Role});
    }
    catch(Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// Get all users
app.MapGet("/api/users", (IMapper _mapper, ApplicationDbContext context) => 
{ 
    try
    {
        var users = _mapper.Map<List<UserResponseDto>>(context.Users.ToList());
        return Results.Ok(users);
    }
    catch(Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Get user by username
app.MapGet("api/user/{username}", ([FromRoute] string username, ApplicationDbContext context) =>
{
    try
    {
        var user = context.Users.FirstOrDefault(u => u.Username == username);
        if (user == null)
        {
            return Results.NotFound();
        }
        return Results.Ok(user);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Updates user status and/or user role
app.MapPut("/api/user", async (ApplicationDbContext context, [FromBody] UserUpdateDto updateUserDto) =>
{
    try
    {
        var user = context.Users.FirstOrDefault(u => u.EmployeeCode == updateUserDto.EmployeeCode);
        if (user == null)
            return Results.NotFound();

        user.Status = updateUserDto.Status;
        if(! new[] { "Admin", "Agente", "Grua" }.Contains(updateUserDto.Role))
        {
            return Results.BadRequest("Rol no es valido");
        }
        user.Role = updateUserDto.Role;
        if (updateUserDto.Role == "Grua")
        {
            if (String.IsNullOrEmpty(updateUserDto.CraneCompany))
                return Results.BadRequest("Si el rol es grua debe tener una empresa de grua");
            user.CraneCompany = updateUserDto.CraneCompany;
        }
        await context.SaveChangesAsync();
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// Updates user password
app.MapPut("/api/user/changePassword", async ([FromBody] ChangePasswordDto CPDto, ApplicationDbContext context) =>
{
    try
    {
        var user = context.Users.FirstOrDefault(u => u.Username == CPDto.Username);
        if(user == null)
        {
            return Results.NotFound();
        }
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(CPDto.Password);
        await context.SaveChangesAsync();
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Deletes user by username
app.MapDelete("/api/user/{username}", async ([FromRoute] string username, ApplicationDbContext context) =>
{
    try
    {
        var user = context.Users.FirstOrDefault(u => u.Username == username);
        if(user == null)
        {
            return Results.NotFound();
        }
        context.Users.Remove(user);
        await context.SaveChangesAsync();
        return Results.Ok(new { Message = "Se borro correctamente" } );
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// CIUDADANOS

// Get all citizens
app.MapGet("/api/citizens", (ApplicationDbContext context, IMapper _mapper) =>
{
    try
    {
        var citizens = context.Citizens.ToList();

        var citizenDtos = citizens.Select(c =>
        {
            var citizenDto = _mapper.Map<CitizenDto>(c);
            citizenDto.Vehicles = context.Vehicles.Where(v => v.GovernmentId == citizenDto.GovernmentId).ToList();
            return citizenDto;
        }).ToList();

        return Results.Ok(citizenDtos);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Add new citizen to the database, receives CitizenDto
app.MapPost("/api/citizen/register", async ([FromBody] CitizenDto citizenDto, ApplicationDbContext context,
    IValidator<CitizenDto> validatorCitizen, IValidator<CitizenVehicle> validatorCitizenVehicle, IMapper _mapper) =>
{
    try
    {
        var validationResult = validatorCitizen.Validate(citizenDto);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }
        foreach (var citizenVehicle in citizenDto.Vehicles!)
        {
            var validation = validatorCitizenVehicle.Validate(citizenVehicle);
            if (!validation.IsValid)
                return Results.BadRequest(validation.Errors);
        }
        var citizenExist = context.Citizens.FirstOrDefault(c => c.GovernmentId == citizenDto.GovernmentId);

        if (citizenExist != null && citizenExist.Status != "No aprobado")
            return Results.Conflict(new { Message = "Este ciudadano ya existe" });

        if (citizenExist != null && citizenExist.Status == "No aprobado")
            context.Citizens.Remove(citizenExist);

        var citizen = _mapper.Map<Citizen>(citizenDto);
        citizen.PasswordHash = BCrypt.Net.BCrypt.HashPassword(citizenDto.Password);
        citizen.Status = "Nuevo";
        context.Citizens.Add(citizen);
        foreach (var vehicle in citizen.Vehicles!)
        {
            vehicle.Status = "Nuevo";
            context.Vehicles.Add(vehicle);
        }
        await context.SaveChangesAsync();
        return Results.Ok(citizen);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// Login citizen
app.MapPost("/api/citizen/login", ([FromBody] CitizenLoginDto user, ApplicationDbContext context, TokenService tokenService) =>
{
    try
    {
        var citizen = context.Citizens.FirstOrDefault(c => c.GovernmentId == user.GovernmentId);
        if(citizen == null)
        {
            return Results.NotFound();
        }
        if (!BCrypt.Net.BCrypt.Verify(user.Password, citizen.PasswordHash))
        {
            return Results.Conflict(new { Message = "Cedula y/o contraseña incorrectos." });
        }
        if (citizen.Status == "Nuevo")
        {
            return Results.Conflict(new { Message = "Ciudadano aun no esta activo, espere a ser aceptado." });
        }
        else if (citizen.Status == "No aprobado")
        {
            return Results.Conflict(new { Message = "El ciudadano no fue aprobado, debes volver a registrarte." });
        }
        else if(citizen.Status == "Aprobado")
        {
            var token = tokenService.GenerateToken(user);
            return Results.Ok(token);
        }
        else
        {
            return Results.Conflict("El estatus no es valido!");
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// Deletes citizen by governmentId
app.MapDelete("/api/citizen/{governmentId}", async (ApplicationDbContext context, [FromRoute] string governmentId) => 
{
    try
    {
        governmentId = governmentId.Replace("-", "");

        if (!Regex.IsMatch(governmentId, @"^[0-9]{11}$"))
            return Results.BadRequest("La cedula no es valida");

        var citizen = context.Citizens.FirstOrDefault(c => c.GovernmentId == governmentId);

        if (citizen == null)
            return Results.NotFound();

        var citizenVehicles = context.Vehicles;
        if(citizenVehicles != null)
            context.Vehicles.RemoveRange(citizenVehicles!);
        context.Citizens.Remove(citizen);
        await context.SaveChangesAsync();
        return Results.Ok();
    }
    catch(Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Updates citizen status
app.MapPut("/api/citizen/updateStatus/", async (ApplicationDbContext context, 
        [FromBody] ChangeCitizenStatusDto changeCitizenStatusDto,
        NotificationService notificationService) =>
{
    try
    {
        if (!new[] { "Aprobado", "No aprobado" }.Contains(changeCitizenStatusDto.Status))
        {
            return Results.BadRequest("Estatus no es valido.");
        }
        var citizen = context.Citizens.FirstOrDefault(c => c.GovernmentId == changeCitizenStatusDto.GovernmentId);
        if (citizen == null)
            return Results.NotFound();
        
        citizen.Status = changeCitizenStatusDto.Status;

        var vehicles = context.Vehicles.Where(v => v.GovernmentId == changeCitizenStatusDto.GovernmentId);

        foreach(var vehicle in vehicles)
        {
            vehicle.Status = "Aprobado";
        }

        if(citizen.Status == "Aprobado" && !String.IsNullOrEmpty(citizen.NotificationToken))
        {
            await notificationService.SendNotificationAsync(citizen.NotificationToken, "Estado de solicitud", "Tu estado ha sido actualizado a 'Aprobado'. Ya puedes iniciar sesión.");
        }
        else if (citizen.Status == "No aprobado" && !String.IsNullOrEmpty(citizen.NotificationToken))
        {
            await notificationService.SendNotificationAsync(citizen.NotificationToken, "Estado de solicitud", "Tu estado ha sido actualizado a 'No aprobado'. Por favor haz el registro de nuevo.");
        }

        await context.SaveChangesAsync();
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Token service for citizen -> No needed anymore
app.MapPut("/api/citizen/updateNotificationToken", async (ApplicationDbContext context, 
        [FromBody] UpdateNotificationTokenDto updateNotificationTokenDto) =>
{
    try
    {
        var citizen = await context.Citizens.FirstOrDefaultAsync(c => c.GovernmentId == updateNotificationTokenDto.GovernmentId);
        if (citizen == null)
            return Results.NotFound("Ciudadano no encontrado.");

        citizen.NotificationToken = updateNotificationTokenDto.NotificationToken;

        await context.SaveChangesAsync();
        return Results.Ok("Token de notificación actualizado correctamente.");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
.RequireAuthorization();

// Citizen vehicles

// Get all vehicles from citizens
app.MapGet("/api/citizen/vehicles", (ApplicationDbContext context) =>
{
    try
    {
        return Results.Ok(context.Vehicles.ToList());
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Get single vehicle from citizens by governmentId
app.MapGet("/api/citizen/vehicle/{governmentId}", (ApplicationDbContext context, [FromRoute] string governmentId) =>
{
    try
    {
        var citizen = context.Citizens.FirstOrDefault(c => c.GovernmentId == governmentId);

        if (citizen == null)
            return Results.NotFound();

        var licensePlates = context.Vehicles.Where(v => v.GovernmentId == citizen.GovernmentId && v.Status == "Aprobado").Select(v => v.LicensePlate).ToList();

        return Results.Ok(licensePlates);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Add vehicle
app.MapPost("/api/citizen/vehicle", async (ApplicationDbContext context, [FromBody] CitizenVehicle citizenVehicle,
    IValidator<CitizenVehicle> validator) =>
{
    try
    {
        var validationResult = validator.Validate(citizenVehicle);

        if (!validationResult.IsValid)
            return Results.BadRequest(validationResult.Errors);

        var vehicle = context.Vehicles.FirstOrDefault(v => v.LicensePlate == citizenVehicle.LicensePlate || v.RegistrationDocument == citizenVehicle.RegistrationDocument);

        if (vehicle != null)
            return Results.Conflict(new { Message = "Ya existe un vehiculo con esta placa o matricula" });

        var citizen = context.Citizens.FirstOrDefault(c => c.GovernmentId == citizenVehicle.GovernmentId);

        if (citizen == null)
            return Results.Conflict(new { Message = "No existe un ciudadano con esta cedula" });

        citizenVehicle.Status = "Nuevo";
        context.Vehicles.Add(citizenVehicle);
        await context.SaveChangesAsync();
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Update vehicle status to: 'Aprobado' or 'No aprobado'
app.MapPut("/api/citizen/vehicles/changeStatus", async (ApplicationDbContext context, ChangeVehicleStatusDto CVSDto) =>
{
    try
    {
        var vehicle = context.Vehicles.FirstOrDefault(v => v.LicensePlate ==  CVSDto.LicensePlate);
        if (vehicle == null)
            return Results.NotFound();

        if (!new[] { "Aprobado", "No aprobado" }.Contains(CVSDto.Status))
            return Results.BadRequest("Estatus no es valido.");

        vehicle.Status = CVSDto.Status;
        await context.SaveChangesAsync();

        return Results.Ok(new { Message = "Estatus actualizado con exito" });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Deletes vehicle by licensePlate
app.MapDelete("/api/citizen/vehicles/delete/{licensePlate}", async (ApplicationDbContext context, [FromRoute] string licensePlate) =>
{
    try
    {
        var vehicle = context.Vehicles.FirstOrDefault(v => v.LicensePlate == licensePlate);
        if (vehicle == null)
            return Results.NotFound();

        context.Vehicles.Remove(vehicle);
        await context.SaveChangesAsync();

        return Results.Ok(new { Message = "Se elimino el vehiculo" });

    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Crane company

// Get all crane companies
app.MapGet("/api/craneCompanies/", (ApplicationDbContext context) =>
{
    try
    {
        var craneCompanies = context.CraneCompanies.ToList();
        return Results.Ok(craneCompanies);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Get only company name from all crane companies
app.MapGet("/api/craneCompaniesList/", (ApplicationDbContext context) =>
{
    try
    {
        var craneCompanies = context.CraneCompanies.Select(c => c.CompanyName).ToList();
        return Results.Ok(craneCompanies);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Get single crane company by RNC
app.MapGet("/api/craneCompany/{rnc}", (ApplicationDbContext context, [FromRoute] string rnc) =>
{
    try
    {
        var craneCompany = context.CraneCompanies.FirstOrDefault(c => c.RNC == rnc);
        return craneCompany == null ?
        Results.NotFound() : 
        Results.Ok(craneCompany);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// Add new crane company to the database
app.MapPost("/api/craneCompany/", async ([FromBody] CraneCompany craneCompany, ApplicationDbContext context) =>
{
    try
    {
        var exist = context.CraneCompanies.FirstOrDefault(c => c.RNC == craneCompany.RNC);
        if (exist != null)
            return Results.Conflict(new {Message = "ya existe"});
        if(String.IsNullOrEmpty(craneCompany.RNC) || String.IsNullOrEmpty(craneCompany.CompanyName) || String.IsNullOrEmpty(craneCompany.PhoneNumber))
        {
            return Results.BadRequest("Se debe recibir RNC, CompanyName y PhoneNumber");
        }
        craneCompany.AmountCraneAgents = 0;
        context.CraneCompanies.Add(craneCompany);
        await context.SaveChangesAsync();
        return Results.Ok(new { Message = "Se agrego correctamente" } );
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

// deletes crane company by RNC
// UPDATE NEEDED -> deletes all crane users if matches the crane company name
app.MapDelete("/api/craneCompany/{rnc}", async (ApplicationDbContext context, [FromRoute] string rnc) =>
{
    try
    {
        var craneCompany = context.CraneCompanies.FirstOrDefault(c => c.RNC == rnc);

        if (craneCompany == null)
            return Results.NotFound();

        context.CraneCompanies.Remove(craneCompany);
        await context.SaveChangesAsync();
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
    .RequireAuthorization();

app.Run();