DhcpCheck
=========

Dhcp Checker sending HDCP DISCOVER messages and reading back OFFERs, can be used to detect rogue DHCP servers.

This is at the moment a first shot with a quick and rather dirty implementation.

If there is public interest rising up with such a tool I will cleanup and extend the tool.


Licensing
=========

Code released under BSD 3 Clause license.


Wireshark settings
==================

	bootp.option.type == 53	-> DHCP packets
