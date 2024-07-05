import 'dart:convert';
import 'package:frontend_android_ciudadano/Data/Models/user_model.dart';
import 'package:http/http.dart' as http;
import 'package:logger/logger.dart';

class RegisterApi {
  final Logger _logger = Logger();

  Future<bool> register(User user) async {
    final response = await http.post(
      Uri.parse('http://192.168.0.168:8089/api/citizen/register'),
      headers: <String, String>{
        'Content-Type': 'application/json; charset=UTF-8',
      },
      body: jsonEncode(user.toJson()),
    );

    _logger.i('Request body: ${jsonEncode(user.toJson())}');
    _logger.i('Response status: ${response.statusCode}');
    _logger.i('Response body: ${response.body}');

    if (response.statusCode == 200) {
      return true;
    } else {
      return false;
    }
  }
}
