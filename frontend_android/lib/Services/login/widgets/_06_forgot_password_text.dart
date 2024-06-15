import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

class ForgotPasswordText extends StatelessWidget {
  const ForgotPasswordText({super.key});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () {
        Navigator.of(context).pushNamed('/Forgot');
      },
      child: Text(
        '¿Olvidaste La Contraseña?',
        style: TextStyle(
          color: Colors.white,
          fontSize: 12.h,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }
}
