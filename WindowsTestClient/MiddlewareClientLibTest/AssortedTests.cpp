#include "stdafx.h"
#include <boost\test\unit_test.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/algorithm/string/replace.hpp>

namespace
{
	struct TestClass
	{
		TestClass(const std::string& data) : somedata(data) {}
		std::string somedata;
	};

	typedef boost::shared_ptr<TestClass> TestClassPtr_t;
	typedef std::map<int, TestClassPtr_t> testLookup_t;
	typedef std::pair<int, TestClassPtr_t> TestPair_t;

	bool RemoveHello(const testLookup_t::value_type& p)
	{
		return p.second->somedata == "hello";
	}
}

BOOST_AUTO_TEST_SUITE(assorted_tests)

BOOST_AUTO_TEST_CASE(test_remove_if)
{
	TestClassPtr_t ptr1(new TestClass("hello"));
	TestClassPtr_t ptr2(new TestClass("goodbye"));

	testLookup_t lookup;
	lookup.insert(testLookup_t::value_type(123, ptr1));
	lookup.insert(testLookup_t::value_type(75, ptr2));

	testLookup_t::iterator i = lookup.begin();
	while ((i = std::find_if(i, lookup.end(), RemoveHello)) != lookup.end())
		lookup.erase(i++);

	BOOST_CHECK(1, lookup.size());
}

BOOST_AUTO_TEST_SUITE_END()