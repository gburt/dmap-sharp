
all: $(ASSEMBLIES)

ASSEMBLIES = client.exe mirror.exe update-test.exe

CLIENTSOURCES = $(srcdir)/SampleClient.cs
SERVERSOURCES = $(srcdir)/SampleServer.cs
MIRRORSOURCES = $(srcdir)/Mirror.cs
UPDATETESTSOURCES = $(srcdir)/UpdateTest.cs

if HAVE_ENTAGGED
ASSEMBLIES += server.exe
endif

client.exe: $(CLIENTSOURCES) $(top_builddir)/src/daap-sharp.dll
	$(MCS) -debug -out:$@ $(CLIENTSOURCES) -r:$(top_builddir)/src/daap-sharp.dll

entagged-sharp.dll:
	cp `pkg-config --variable=Libraries entagged-sharp` .

server.exe: $(SERVERSOURCES) $(top_builddir)/src/daap-sharp.dll entagged-sharp.dll
	$(MCS) -debug -out:$@ $(SERVERSOURCES) -r:$(top_builddir)/src/daap-sharp.dll -r:entagged-sharp.dll

mirror.exe: $(MIRRORSOURCES) $(top_builddir)/src/daap-sharp.dll
	$(MCS) -debug -out:$@ $(MIRRORSOURCES) -r:$(top_builddir)/src/daap-sharp.dll

update-test.exe: $(UPDATETESTSOURCES) $(top_builddir)/src/daap-sharp.dll
	$(MCS) -debug -out:$@ $(UPDATETESTSOURCES) -r:$(top_builddir)/src/daap-sharp.dll

EXTRA_DIST = $(CLIENTSOURCES) $(SERVERSOURCES) $(MIRRORSOURCES) $(UPDATETESTSOURCES)

CLEANFILES = $(ASSEMBLIES) entagged-sharp.dll *.mdb
