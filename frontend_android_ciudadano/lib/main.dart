import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:frontend_android_ciudadano/Api/ConsultaDePlacas/_00_api_consulta_placa.dart';
import 'package:frontend_android_ciudadano/Data/Blocs/Login/LoginLogic/_02_login_bloc.dart';
import 'package:frontend_android_ciudadano/Data/Blocs/Vehiculo/_00_vehicle_event.dart';
import 'package:frontend_android_ciudadano/Data/Blocs/Vehiculo/_02_vehicle_bloc.dart';
import 'package:frontend_android_ciudadano/Views/Login/_00_login.dart';

void main() => runApp(const M());

class M extends StatelessWidget {
  const M({super.key});

  @override
  Widget build(BuildContext context) {
    return ScreenUtilInit(
      designSize: const Size(360, 640),
      builder: (context, child) {
        return MultiBlocProvider(
          providers: [
            BlocProvider<LoginBloc>(
              create: (context) => LoginBloc(),
            ),
            BlocProvider<VehicleBloc>(
              create: (context) =>
                  VehicleBloc(ConsultaPlaca())..add(FetchLicencePlates()),
            )
          ],
          child: MaterialApp(
            debugShowCheckedModeBanner: false,
            home: Login(),
          ),
        );
      },
    );
  }
}
