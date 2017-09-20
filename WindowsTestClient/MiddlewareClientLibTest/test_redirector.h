#pragma once

#include <iostream>
#include <fstream>

#include <boost/test/results_reporter.hpp>

std::ofstream out;

extern const char* test_file_name;

struct report_redirector
{
	report_redirector()
	{
		out.open(test_file_name);
		assert(out.is_open());
		boost::unit_test::results_reporter::set_stream(out);
	}
};


