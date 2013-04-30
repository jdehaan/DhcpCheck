DhcpCheck
=========

Dhcp Checker sending HDCP DISCOVER messages and reading back OFFERs, can be used to detect rogue DHCP servers.

This is at the moment a first shot with a quick and rather dirty implementation.

If there is public interest rising up with such a tool I will cleanup and extend the tool.

Prerequisities
==============

.NET4 must be installed, web installer here: http://www.microsoft.com/en-us/download/details.aspx?id=17851

WinPCap must be installed, get it here: http://www.winpcap.org/


Licensing
=========

Code released under BSD 3 Clause license.


Wireshark settings
==================

**Filter out the Boostrap packets containing DHCP data**

	bootp.option.type == 53
