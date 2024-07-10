import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_svg/svg.dart';
import 'package:image_picker/image_picker.dart';
import 'package:frontend_android/Pages/Confirmation/_03_confirmation.dart';

const Color lightBlueColor = Color(0xFF009DD4); // Azul Claro
const Color darkBlueColor = Color(0xFF010F56); // Azul Oscuro
const Color greyTextColor = Color(0xFF494A4D); // Gris (Texto)

class PhotoScreen extends StatefulWidget {
  final String plateNumber;
  final String vehicleType;
  final String color;
  final String address;
  final String? latitude;
  final String? longitude;

  const PhotoScreen({
    super.key,
    required this.plateNumber,
    required this.vehicleType,
    required this.color,
    required this.address,
    required this.latitude,
    required this.longitude,
  });

  @override
  NewReportPhotoScreenState createState() => NewReportPhotoScreenState();
}

class NewReportPhotoScreenState extends State<PhotoScreen> {
  final ImagePicker _picker = ImagePicker();
  final List<XFile> _imageFileList = [];

  void _pickImages() async {
    if (_imageFileList.length < 5) {
      final XFile? image = await _picker.pickImage(
        source: ImageSource.camera,
        imageQuality: 50,
        preferredCameraDevice: CameraDevice.rear,
      );

      if (image != null) {
        setState(() {
          _imageFileList.add(image);
        });
      }
    }
  }

  void _navigateToConfirmation() {
    if (_imageFileList.length >= 3) {
      Navigator.pushReplacement(
        context,
        MaterialPageRoute(
          builder: (context) => ConfirmationScreen(
            plateNumber: widget.plateNumber,
            vehicleType: widget.vehicleType,
            color: widget.color,
            address: widget.address,
            latitude: widget.latitude,
            longitude: widget.longitude,
            imageFileList: _imageFileList,
          ),
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Debe agregar al menos 3 fotos')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return WillPopScope(
      onWillPop: () async {
        Navigator.of(context).pop();
        return true;
      },
      child: Scaffold(
        backgroundColor: const Color(0xFFFFFFFF), // Fondo blanco
        body: SafeArea(
          child: Padding(
            padding: EdgeInsets.symmetric(horizontal: 14.h),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SizedBox(height: 10.h),
                Center(
                  child: Text(
                    'Nuevo reporte',
                    style: TextStyle(
                      fontSize: 19.h,
                      fontWeight: FontWeight.bold,
                      color: lightBlueColor,
                    ),
                  ),
                ),
                SizedBox(height: 18.h),
                Center(
                  child: Text(
                    'Fotos del vehículo',
                    style: TextStyle(
                        fontSize: 14.h,
                        fontWeight: FontWeight.bold,
                        color: greyTextColor),
                  ),
                ),
                SizedBox(height: 14.h),
                Center(
                  child: FotoButton(
                    onTap: _pickImages,
                    iconPath: 'assets/icons/add.svg',
                    title: 'Agregar foto',
                  ),
                ),
                SizedBox(height: 15.h),
                Expanded(
                  child: _imageFileList.isNotEmpty
                      ? ListView.builder(
                          itemCount: _imageFileList.length,
                          itemBuilder: (context, index) {
                            return Padding(
                              padding: EdgeInsets.symmetric(
                                horizontal: 0.h,
                                vertical: 4.h,
                              ),
                              child: Stack(
                                children: [
                                  Container(
                                    width: double.infinity,
                                    decoration: BoxDecoration(
                                      border: Border.all(
                                        color: const Color(0xFF010F56),
                                        width:
                                            3.0, // Grosor del borde aumentado
                                      ),
                                      borderRadius: BorderRadius.circular(15.0),
                                    ),
                                    child: ClipRRect(
                                      borderRadius: BorderRadius.circular(15.0),
                                      child: Image.file(
                                        File(_imageFileList[index].path),
                                        width: double.infinity,
                                        height: 100.h,
                                        fit: BoxFit.cover,
                                      ),
                                    ),
                                  ),
                                  Positioned(
                                    top: 5,
                                    right: 5,
                                    child: GestureDetector(
                                      onTap: () {
                                        setState(() {
                                          _imageFileList.removeAt(index);
                                        });
                                      },
                                      child: SvgPicture.asset(
                                          'assets/icons/editar.svg',
                                          height: 24.h,
                                          width: 24.w),
                                    ),
                                  ),
                                ],
                              ),
                            );
                          },
                        )
                      : Center(
                          child: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              SvgPicture.asset(
                                'assets/icons/photo.svg',
                                height: 30.h,
                              ),
                              SizedBox(height: 5.h),
                              Text(
                                'Sin fotos agregadas',
                                style: TextStyle(
                                  color: Colors.grey,
                                  fontSize: 10.h,
                                ),
                              ),
                            ],
                          ),
                        ),
                ),
                SizedBox(height: 20.h),
                Padding(
                  padding: EdgeInsets.symmetric(horizontal: 4.h),
                  child: SizedBox(
                    width: double.infinity,
                    child: ElevatedButton(
                      onPressed: _navigateToConfirmation,
                      style: ElevatedButton.styleFrom(
                        padding: EdgeInsets.symmetric(vertical: 14.h),
                        backgroundColor: const Color(0xFF010F56),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10.h),
                        ),
                      ),
                      child: Text(
                        'Finalizar',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 16.h,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ),
                ),
                SizedBox(height: 20.h),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class FotoButton extends StatelessWidget {
  final VoidCallback onTap;
  final String iconPath;
  final String title;

  const FotoButton({
    super.key,
    required this.onTap,
    required this.iconPath,
    required this.title,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(8.0),
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: 14.h, vertical: 10.w),
        decoration: BoxDecoration(
          border: Border.all(color: lightBlueColor, width: 1.3.h),
          borderRadius: BorderRadius.circular(10.0),
        ),
        child: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              SvgPicture.asset(
                iconPath,
                height: 24.h,
                colorFilter: const ColorFilter.mode(
                  lightBlueColor,
                  BlendMode.srcIn,
                ),
              ),
              SizedBox(height: 8.h),
              Text(
                title,
                style: TextStyle(
                  color: darkBlueColor,
                  fontSize: 14.h,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
