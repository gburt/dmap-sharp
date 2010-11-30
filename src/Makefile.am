ASSEMBLY_NAME=dmap-sharp
DMAP_ASSEMBLY = dmap-sharp.dll
ASSEMBLY=$(DMAP_ASSEMBLY)
ASSEMBLIES = $(DMAP_ASSEMBLY)

dmapdir = $(DESTDIR)$(prefix)/lib/mono/dmap-sharp
dmap_SCRIPTS = $(DMAP_ASSEMBLY).mdb

DMAPSOURCES =				\
	$(srcdir)/Daap/Client.cs		\
	$(srcdir)/Daap/Database.cs \
	$(srcdir)/Daap/Playlist.cs \
	$(srcdir)/Daap/Server.cs \
	$(srcdir)/Daap/Track.cs \
	$(srcdir)/Dacp/Server.cs		\
	$(srcdir)/Dmap/AuthenticationException.cs	\
	$(srcdir)/Dmap/BrokenMD5.cs		\
	$(srcdir)/Dmap/ContentParser.cs	\
	$(srcdir)/Dmap/ContentCodeBag.cs 	\
	$(srcdir)/Dmap/ContentFetcher.cs 	\
	$(srcdir)/Dmap/ContentWriter.cs 	\
	$(srcdir)/Dmap/Hasher.cs		\
	$(srcdir)/Dmap/LoginException.cs	\
	$(srcdir)/Dmap/Server.cs		\
	$(srcdir)/Dmap/ServerInfo.cs		\
	$(srcdir)/Dmap/ServiceLocator.cs	\
	$(srcdir)/Dmap/User.cs		\
	$(srcdir)/Dmap/Utility.cs

if HAVE_MONODOC
DOC_PACKAGE=dmap-sharp-docs

monodocdir = $(DOCDIR)
monodoc_DATA = \
	$(DOC_PACKAGE).zip \
	$(DOC_PACKAGE).tree \
	$(DOC_PACKAGE).source

$(DOC_PACKAGE).tree: $(DOC_PACKAGE).zip

$(DOC_PACKAGE).zip: $(srcdir)/en/*/*.xml $(srcdir)/en/*.xml
	$(MDASSEMBLER) --format=ecma --out=$(DOC_PACKAGE) $(srcdir)/en

endif

docs2html:
	monodocs2html -source:en -dest:html

update-docs: $(DMAP_ASSEMBLY)
	$(MONODOCER) -delete -assembly:$(DMAP_ASSEMBLY) -path:en

all: $(ASSEMBLIES)

$(ASSEMBLY).mdb: $(ASSEMBLY)

$(ASSEMBLY): $(DMAPSOURCES)
	$(MCS) $(MCS_FLAGS) -keyfile:$(srcdir)/dmap-sharp.snk -debug -target:library -out:$@ $(DMAPSOURCES) -resource:$(srcdir)/content-codes,content-codes $(MCS_FLAGS) -r:ICSharpCode.SharpZipLib

install-data-local:
	echo "$(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS)";  \
        $(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS) || exit 1;

uninstall-local:
	echo "$(GACUTIL) /u $(ASSEMBLY_NAME) /package $(ASSEMBLY_NAME) /root $(DESTDIR)$(libdir)"; \
        $(GACUTIL) /u $(ASSEMBLY_NAME) /package $(ASSEMBLY_NAME) /root $(DESTDIR)$(libdir) || exit 1;

EXTRA_DIST =			\
	$(DMAPSOURCES)		\
	dmap-sharp.snk		\
	content-codes		\
	$(DOC_PACKAGE).source	\
	$(srcdir)/en/*.xml	\
	$(srcdir)/en/*/*.xml

DISTCLEANFILES = \
	$(DOC_PACKAGE).zip \
	$(DOC_PACKAGE).tree

CLEANFILES = $(ASSEMBLIES) *.mdb
