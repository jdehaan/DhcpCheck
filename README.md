DhcpCheck
=========

DhcpCheck is sending HDCP DISCOVER messages at constant intervals and reading back OFFERs, thus
it can be used to detect rogue DHCP servers. The tool also logs out all visible DHCP packets
coming to the NICs of the computer running the tool. The data is logged in CSV and as a pcap file
as well. The pcap file can be read by wireshark for example for getting full packet information.

This is at the moment a first shot with a quick and rather dirty implementation. Wireshark was
not able to parse for a very long time (crashes) and the capture settings limitating the amount
of packets to be captured didn't work out for some reason.

If there is public interest rising up with such a tool I will cleanup and extend the tool.

Prerequisities
==============

.NET4 must be installed, web installer here: http://www.microsoft.com/en-us/download/details.aspx?id=17851

WinPCap must be installed, get it here: http://www.winpcap.org/


Licensing
=========

DhcpCheck Code released under LPGL v3

DhcpCheck makes use of:

- PacketDotNet		LGPL v3
- SharpPcap			LGPL v3

I plan to contribute a BootpPacket / DhcpPacket class to the PacketDotNet project, which is a cleaner place
and way to implement the packet decoding than the current situation.


Wireshark settings
==================

**Filter out the Boostrap packets containing DHCP data**

	bootp.option.type == 53
