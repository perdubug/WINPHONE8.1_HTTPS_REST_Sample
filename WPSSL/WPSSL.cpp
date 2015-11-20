#include "pch.h"

#include "openssl\ssl.h"
#include "openssl\bio.h"
#include "openssl\err.h"
#include "openssl\x509.h"
#include "openssl\x509_vfy.h"

#include "WPSSL.h"

#pragma comment(lib, "ssleay32.lib")
#pragma comment(lib, "libeay32.lib")
#pragma comment(lib, "ws2_32.lib")

using namespace WPSSL;
using namespace Platform;
using namespace Windows;

#define MAX_HTTPREQ_SIZE 1024

#define baseSSLUrl "<REST API HOST URL>"
#define userName  "<USER_NAME>"
#define userPwd  "<PASSWORD>"
#define clientId  "<CLIENT_ID>"
#define phrasekey "<PHRASE_KEY>"

String^ WPSSLImpl::GetAuthCode(String^ capem, String^ clpem)
{
	BIO * bio = NULL;
	SSL * ssl = NULL;
	SSL_CTX * ctx = NULL;
	
	size_t i;
	char strHttpReq[MAX_HTTPREQ_SIZE] = { 0 };
	char strCAPEMFilePath[_MAX_PATH] = { 0 };
	char strCLPEMFilePath[_MAX_PATH] = { 0 };	
	int readIndex;
	char respBuf[1024] = {0};
	char errmsg[768] = { 0 };	

	X509 * cert = NULL;	
	long ret = 0;
	char * pcursor = 0;

	auto uri = baseSSLUrl + "?client_id=" + clientId + "&response_type=code&user=" + userName + "&pwd=" + userPwd;
	auto httphead = "GET " + uri + " HTTP/1.1" + "\x0D\x0A";
	httphead += "Host: serverCertificateName" + "\x0D\x0A";
	httphead += "Connection: close" + "\x0D\x0A";
	httphead += "User-Agent: CSharp" + "\x0D\x0A";
	httphead += "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8" + "\x0D\x0A";
	httphead += "Accept-Encoding: gzip,deflate,sdch" + "\x0D\x0A";
	httphead += "Accept-Language: zh-CN,en-US,en;q=0.8" + "\x0D\x0A";
	httphead += "Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.3" + "\x0D\x0A";
	httphead += "\x0D\x0A";

	wcstombs_s(&i, strHttpReq, MAX_HTTPREQ_SIZE, httphead->Data(), httphead->Length());
	wcstombs_s(&i, strCAPEMFilePath, _MAX_PATH, capem->Data(), capem->Length());
	wcstombs_s(&i, strCLPEMFilePath, _MAX_PATH, clpem->Data(), clpem->Length());	

	//Initializing everything...
	SSL_library_init();
	SSL_load_error_strings();	
	ERR_load_BIO_strings();
	ERR_load_crypto_strings();
	OpenSSL_add_all_algorithms();	

	//
	// Creating and Setting Up the SSL Context Structure (SSL_CTX)
	//
	ctx = SSL_CTX_new(SSLv23_method());
	if (!ctx)  {
		sprintf_s(errmsg, "ERR:SSL_CTX_new Error:%s\n", ERR_reason_error_string(ERR_get_error()));
		goto GO_EXIT;
	}

	if (!SSL_CTX_load_verify_locations(ctx, strCAPEMFilePath, NULL))
	{
		sprintf_s(errmsg, "ERR:SSL_CTX_load_verify_locations:%s\n", ERR_reason_error_string(ERR_get_error()));
		goto GO_EXIT;
	}

	if (!SSL_CTX_use_certificate_file(ctx, strCLPEMFilePath, SSL_FILETYPE_PEM))
	{
		sprintf_s(errmsg, "ERR:SSL_CTX_use_certificate_file:%s\n", ERR_reason_error_string(ERR_get_error()));
		goto GO_EXIT;
	}
	
	SSL_CTX_set_default_passwd_cb_userdata(ctx, phrasekey);
	if(!SSL_CTX_use_RSAPrivateKey_file(ctx,strCLPEMFilePath, SSL_FILETYPE_PEM))
	{
		sprintf_s(errmsg, "ERR:SSL_CTX_set_default_passwd_cb_userdata:%s\n", ERR_reason_error_string(ERR_get_error()));
		goto GO_EXIT;
	}

	// Creating the connection
	bio = BIO_new_ssl_connect(ctx);	
	BIO_get_ssl(bio, &ssl);
	if (!ssl) {
		sprintf_s(errmsg, "ERR:BIO_new_ssl_connect:%s\n", ERR_reason_error_string(ERR_get_error()));
		goto GO_EXIT;
	}

	SSL_set_mode(ssl, SSL_MODE_AUTO_RETRY);	

	// Create and setup the connection
	BIO_set_conn_hostname(bio, "kk.bigk2.com:8443");

	// Verify the connection opened and perform the handshake
	if (BIO_do_connect(bio) <= 0)
	{
		sprintf_s(errmsg, "ERR:BIO_do_connect:%s\n", ERR_reason_error_string(ERR_get_error()));
		goto GO_EXIT;
	}

	ret = SSL_get_verify_result(ssl);
	if (ret != X509_V_OK)
	{
		sprintf_s(errmsg, "ERROR:%i,%s\n", ret, X509_verify_cert_error_string(ret));
		//DO NOT EXIT HERE!!!
		//because if you are using a self-signed certificate you will receive 18 or 20
		//18 X509_V_ERR_DEPTH_ZERO_SELF_SIGNED_CERT which is not an error
		//20 Not verified Issuser, which is normal for no-pay companies
		//goto GO_EXIT;
	}

	// Send the request
	BIO_write(bio, strHttpReq, MAX_HTTPREQ_SIZE);

	// Read in the response and just return whole result to C# for fetch JSON object
	for (;;)
	{
		readIndex = BIO_read(bio, respBuf, 1023);
		if (readIndex <= readIndex) break;
		respBuf[readIndex] = '\0';
		//sprintf_s(errmsg, "%s", respBuf);
	}

GO_EXIT:
	// Close the connection and free the context
	if(cert) X509_free(cert);
	if(bio)  BIO_free_all(bio);
	if(ctx)  SSL_CTX_free(ctx);

	std::string s_str = std::string(respBuf);
	std::wstring wid_str = std::wstring(s_str.begin(), s_str.end());
	const wchar_t* w_char = wid_str.c_str();
	Platform::String^ httpsResp = ref new Platform::String(w_char);

	return httpsResp;
}

