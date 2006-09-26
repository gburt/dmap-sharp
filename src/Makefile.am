
DAAP_ASSEMBLY = daap-sharp.dll

ASSEMBLIES = $(DAAP_ASSEMBLY)

daapdir = $(libdir)/daap-sharp
daap_SCRIPTS = $(ASSEMBLIES) $(DAAP_ASSEMBLY).config $(DAAP_ASSEMBLY).mdb

DAAPSOURCES =				\
	$(srcdir)/AuthenticationException.cs	\
	$(srcdir)/BrokenMD5.cs		\
	$(srcdir)/ContentParser.cs	\
	$(srcdir)/ContentCodeBag.cs 	\
	$(srcdir)/ContentFetcher.cs 	\
	$(srcdir)/ContentWriter.cs 	\
	$(srcdir)/Client.cs		\
	$(srcdir)/Database.cs		\
	$(srcdir)/Hasher.cs		\
	$(srcdir)/LoginException.cs	\
	$(srcdir)/Playlist.cs		\
	$(srcdir)/Server.cs		\
	$(srcdir)/ServerInfo.cs		\
	$(srcdir)/ServiceLocator.cs	\
	$(srcdir)/Track.cs		\
	$(srcdir)/User.cs		\
	$(srcdir)/Utility.cs		\
	$(srcdir)/Mono.Zeroconf/*.cs

if HAVE_MONODOC
docdir = $(MONODOC_DIR)
doc_DATA = daap-sharp-docs.zip daap-sharp-docs.tree daap-sharp-docs.source
DISTCLEANFILES = daap-sharp-docs.zip daap-sharp-docs.tree
endif

docs2html:
	monodocs2html -source:en -dest:html

update-docs: $(DAAP_ASSEMBLY)
	$(MONODOCER) -delete -assembly:$(DAAP_ASSEMBLY) -path:en

daap-sharp-docs.zip: $(srcdir)/en/*/*
	$(MDASSEMBLER) --out daap-sharp-docs --ecma $(srcdir)/en

all: $(ASSEMBLIES)

$(DAAP_ASSEMBLY): $(DAAPSOURCES)
	$(MCS) $(MCS_FLAGS) -debug -keyfile:$(srcdir)/daap-sharp.snk -target:library -out:$@ $(DAAPSOURCES) -resource:$(srcdir)/content-codes,content-codes $(MCS_FLAGS) -r:System.Web -r:ICSharpCode.SharpZipLib

EXTRA_DIST =			\
	$(DAAPSOURCES)		\
	daap-sharp.snk		\
	content-codes		\
	daap-sharp-docs.source	\
	$(srcdir)/en/*.xml	\
	$(srcdir)/en/*/*.xml

CLEANFILES = $(ASSEMBLIES) *.mdb
