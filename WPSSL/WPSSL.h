#include <string>

namespace WPSSL
{
	using namespace Windows::Foundation;
	using Platform::String;

	public ref class WPSSLImpl sealed {
    public:
		String^ GetAuthCode(String^ capem, String^ clpem);
	};
}
