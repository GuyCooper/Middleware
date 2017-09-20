#include "stdafx.h"

#define BOOST_TEST_MODULE middleware_unit_tests
#define BOOST_TEST_MAIN

#include <boost/test/included/unit_test.hpp>

static const char* test_file_name = ".\\middleware_tests.xml";

#include "test_redirector.h"

BOOST_GLOBAL_FIXTURE(report_redirector);