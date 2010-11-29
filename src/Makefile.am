
DMAP_ASSEMBLY = dmap-sharp.dll

ASSEMBLIES = $(DMAP_ASSEMBLY)

dmapdir = $(libdir)/dmap-sharp
dmap_SCRIPTS = $(ASSEMBLIES) $(DMAP_ASSEMBLY).config $(DMAP_ASSEMBLY).mdb

DMAPSOURCES =				\
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
	$(srcdir)/Utility.cs

if HAVE_MONODOC
docdir = $(MONODOC_DIR)
doc_DATA = dmap-sharp-docs.zip dmap-sharp-docs.tree dmap-sharp-docs.source
DISTCLEANFILES = dmap-sharp-docs.zip dmap-sharp-docs.tree
endif

docs2html:
	monodocs2html -source:en -dest:html

update-docs: $(DMAP_ASSEMBLY)
	$(MONODOCER) -delete -assembly:$(DMAP_ASSEMBLY) -path:en

dmap-sharp-docs.zip: $(srcdir)/en/*/*
	$(MDASSEMBLER) --out dmap-sharp-docs --ecma $(srcdir)/en

all: $(ASSEMBLIES)

$(DMAP_ASSEMBLY): $(DMAPSOURCES)
	$(MCS) $(MCS_FLAGS) -debug -target:library -out:$@ $(DMAPSOURCES) -resource:$(srcdir)/content-codes,content-codes $(MCS_FLAGS) -r:System.Web -r:ICSharpCode.SharpZipLib

EXTRA_DIST =			\
	$(DMAPSOURCES)		\
	dmap-sharp.snk		\
	content-codes		\
	dmap-sharp-docs.source	\
	$(srcdir)/en/*.xml	\
	$(srcdir)/en/*/*.xml

CLEANFILES = $(ASSEMBLIES) *.mdb
