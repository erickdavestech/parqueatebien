import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:logger/logger.dart';
import 'package:shared_preferences/shared_preferences.dart';

class AddNewVehicleApi {
  static final Logger _logger = Logger();

  Future<bool> addVehicle({
    required String governmentId,
    required String licensePlate,
    required String registrationDocument,
    required String model,
    required String year,
    required String color,
  }) async {
    const url = 'http://192.168.0.209:8089/api/citizen/vehicle';

    try {
      final token = await _getToken();
      if (token == null) {
        _logger.e('No token found. Please login first.');
        return false;
      }

      final response = await http
          .post(
            Uri.parse(url),
            headers: <String, String>{
              'Content-Type': 'application/json; charset=UTF-8',
              'Authorization': 'Bearer $token',
            },
            body: jsonEncode(<String, String>{
              "governmentId": governmentId,
              "licensePlate": licensePlate,
              "registrationDocument": registrationDocument,
              "model": model,
              "year": year,
              "color": color,
            }),
          )
          .timeout(const Duration(seconds: 60));

      _logger.i('Response status: ${response.statusCode}');
      _logger.i('Response body: ${response.body}');

      if (response.statusCode == 200) {
        return true;
      } else if (response.statusCode == 400) {
        _logger.e('Bad request: ${response.body}');
        return false;
      } else if (response.statusCode == 409) {
        _logger.e('Conflict: ${jsonDecode(response.body)['Message']}');
        return false;
      } else {
        _logger.e('Unexpected error: ${response.statusCode}');
        return false;
      }
    } catch (e) {
      _logger.e('Error adding vehicle: $e');
      return false;
    }
  }

  Future<String?> _getToken() async {
    SharedPreferences prefs = await SharedPreferences.getInstance();
    return prefs.getString('token');
  }
}
