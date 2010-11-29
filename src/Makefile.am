
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

$(DMAP_ASSEMBLY): $(DMAPSOURCES)
	$(MCS) $(MCS_FLAGS) -debug -target:library -out:$@ $(DMAPSOURCES) -resource:$(srcdir)/content-codes,content-codes $(MCS_FLAGS) -r:System.Web -r:ICSharpCode.SharpZipLib

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
