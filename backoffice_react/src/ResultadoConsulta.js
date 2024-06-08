import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import imgBusqueda from './img/BUSQUEDA.svg'
import { Nav } from './nav'
import './resultadoconsulta.css'

export function ResultadoConsulta() {

    const url = "https://demooriontek.azurewebsites.net"
    
    const [vehicleType, setVehicleType] = useState('');
    const [vehicleColor, setVehicleColor] = useState('');
    const [reportedDate, setReportedDate] = useState('');
    const [status, setStatus] = useState('');
    const [address, setAddress] = useState('');
    const [currentAddress, setCurrentAddress] = useState('');
    const [towedByCraneDate, setTowedByCraneDate] = useState('');
    const [arrivalAtParkinglot, setArrivalAtParkinglot] = useState('');
    const [releaseDate, setReleaseDate] = useState('');
    const [images, setImages] = useState([]);

    const navigate = useNavigate();
    const location = useLocation();

    const licensePlate = location?.state?.licensePlate;
    const username = location?.state?.username;

    useEffect(() => {

        const fetchData = async () => {

            const response = await fetch(`${url}/ciudadanos/${licensePlate}`);
            if(response.ok) {
                const data = await response.json();
                setVehicleType(data.VehicleType);
                setVehicleColor(data.VehicleColor);
                setReportedDate(data.ReportedDate);
                setStatus(data.Status);
                setAddress(data.Address);
                setCurrentAddress(data.CurrentAddress);
                setTowedByCraneDate(data.TowedByCraneDate || 'N/A');
                setArrivalAtParkinglot(data.ArrivalAtParkinglot || 'N/A');
                setReleaseDate(data.ReleaseDate || 'N/A');
                setImages(data.Photos);
            }
            else {
                navigate('/resultadoError');
            }
        }
        fetchData()

    }, [])

    const statusClassName = status == 'Reportado' ? 'reportado' :
                            status == 'Incautado por grua' ? 'incautado' :
                            status == 'Retenido' ? 'retenido' :
                            status == 'Liberado' ? 'liberado' : '';

    if(licensePlate == null) {
        navigate('/backoffice');
        return;
    }

    return (
        <>
        <Nav username={username}/>
        <article className="title">
            <h1>RESULTADO DE CONSULTA</h1>
        
            <button onClick = {()=>{navigate('/backoffice', {state: {username: username}})}} id="btnRealizarOtraConsulta">
                <img src={imgBusqueda} />
                Realizar otra consulta</button>
        </article>

        <div className="resultado-consulta">

            <section className="datos">
                <article className="datos-vehiculo">
                    <h3>Datos del vehículo</h3>
                    <div className="dato">
                        <div>
                            <h4 className="subtitleh4">No. de Registro y placa:</h4>
                            <p id="placa">{licensePlate}</p>
                        </div>
                        <div>
                            <h4 className="subtitleh4">Tipo de vehiculo:</h4>
                            <p>{vehicleType}</p>
                        </div>
                        <div>
                            <h4 className="subtitleh4">Color:</h4>
                            <p>{vehicleColor}</p>
                        </div>
                    </div>
                    
                </article>
                <article className="reporte-creado-por">
                    <h3>Reporte creado por</h3>
                    <div className="dato">
                        <div>
                            <h4 className="subtitleh4">Nombre del agente:</h4>
                            <p>Juan Manuel Sanchez Ruiz</p>
                        </div>
                        <div>
                            <h4 className="subtitleh4">Fecha y hora del reporte:</h4>
                            <p>{reportedDate}</p>
                        </div>
                    </div>
                    
                </article>
                <div id="setStatus">
                </div>
                <article className="fotos-vehiculo">
                    <h3>Fotos del vehículo</h3>
                    <div id="imagenes" className="imagenes">
                        {
                            images.map( (photo,index) => 
                                <img key={index} src={`${photo.FileType}${photo.File}`} alt={`Photo ${index}`} />
                            )
                        }
                    </div>
                </article>
            </section>
            <section className="informacion">
                <h3 className="subtitleh3">Información del reporte</h3>
                <h5>Estatus</h5>
                <span id="status" className={`status ${statusClassName}`}>{status}</span>
                <h5>Ubicación de reporte / recogida:</h5>
                <span>{address}</span>
                <h5>Fecha y hora de incautación por grúa:</h5>
                <span>{towedByCraneDate}</span>
                <h5>Ubicación actual:</h5>
                <span>{currentAddress}</span>
                <h5>Fecha y hora de llegada al centro:</h5>
                <span>{arrivalAtParkinglot}</span>
                <h5>Fecha y hora de liberación:</h5>
                <span>{releaseDate}</span>
                <h5>Liberado por:</h5>
                <span>N/A</span>
            </section>
        </div>
    </>
    )
}