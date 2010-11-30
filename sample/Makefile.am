
all: $(ASSEMBLIES)

ASSEMBLIES = client.exe mirror.exe update-test.exe dacp-server.exe

CLIENTSOURCES = $(srcdir)/SampleClient.cs
SERVERSOURCES = $(srcdir)/SampleServer.cs
MIRRORSOURCES = $(srcdir)/Mirror.cs
UPDATETESTSOURCES = $(srcdir)/UpdateTest.cs
DACPSOURCES = $(srcdir)/DacpServer.cs

if HAVE_TAGLIB
ASSEMBLIES += server.exe
endif

client.exe: $(CLIENTSOURCES) $(top_builddir)/src/dmap-sharp.dll
	$(MCS) -debug -out:$@ $(CLIENTSOURCES) -r:$(top_builddir)/src/dmap-sharp.dll

server.exe: $(SERVERSOURCES) $(top_builddir)/src/dmap-sharp.dll
	$(MCS) -debug -out:$@ $(TAGLIB_LIBS) $(SERVERSOURCES) -r:$(top_builddir)/src/dmap-sharp.dll

mirror.exe: $(MIRRORSOURCES) $(top_builddir)/src/dmap-sharp.dll
	$(MCS) -debug -out:$@ $(MIRRORSOURCES) -r:$(top_builddir)/src/dmap-sharp.dll

update-test.exe: $(UPDATETESTSOURCES) $(top_builddir)/src/dmap-sharp.dll
	$(MCS) -debug -out:$@ $(UPDATETESTSOURCES) -r:$(top_builddir)/src/dmap-sharp.dll

dacp-server.exe: $(DACPSOURCES) $(top_builddir)/src/dmap-sharp.dll
	$(MCS) -debug -out:$@ $(DACPSOURCES) -r:$(top_builddir)/src/dmap-sharp.dll

EXTRA_DIST = $(CLIENTSOURCES) $(SERVERSOURCES) $(MIRRORSOURCES) $(UPDATETESTSOURCES)

CLEANFILES = $(ASSEMBLIES) entagged-sharp.dll *.mdb
