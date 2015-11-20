# WINPHONE8.1_HTTPS_REST_Sample
This is a example about how to use HTTPS REST APIs on Windows Phone 8.1 by using MS OpenSSL fork(see https://github.com/Microsoft/openssl/)

Please change below lines in WPSSL\WPSSL.cpp based on your case
--------------------------------------------------------------------
21 #define baseSSLUrl "<REST API HOST URL>" 
22 #define userName  "<USER_NAME>" 
23 #define userPwd  "<PASSWORD>" 
24 #define clientId  "<CLIENT_ID>" 
25 #define phrasekey "<PHRASE_KEY>"
