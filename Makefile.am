SUBDIRS = src sample

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = dmap-sharp.pc

release: dist
	svn cp svn+ssh://jwilcox@mono-cvs.ximian.com/source/trunk/dmap-sharp \
        svn+ssh://jwilcox@mono-cvs.ximian.com/source/tags/dmap-sharp-$(VERSION) -m "tag for $(VERSION)"
	scp dmap-sharp-$(VERSION).tar.gz root@snorp.net:/var/www/files/dmap-sharp

winbuild: all
	make distclean
	./autogen.sh --enable-mdnsd
	make
	make dist
	rm -rf dmap-sharp-$(VERSION)
	rm -f dmap-sharp-$(VERSION).zip
	tar xvfz dmap-sharp-$(VERSION).tar.gz
	zip -r dmap-sharp-$(VERSION).zip dmap-sharp-$(VERSION)
	rm -rf dmap-sharp-$(VERSION)
	mkdir dmap-sharp-$(VERSION)
	cp src/dmap-sharp.dll dmap-sharp-$(VERSION)
	cp sample/*.exe dmap-sharp-$(VERSION)
	cp sample/entagged-sharp.dll dmap-sharp-$(VERSION)
	cp `pkg-config --variable=libdir mono`/mono/1.0/ICSharpCode.SharpZipLib.dll dmap-sharp-$(VERSION)
	zip -r dmap-sharp-$(VERSION)-binary.zip dmap-sharp-$(VERSION)
	rm -rf dmap-sharp-$(VERSION)
	make distclean

winrelease: winbuild
	scp dmap-sharp-$(VERSION)-binary.zip root@snorp.net:/var/www/files/dmap-sharp
	scp dmap-sharp-$(VERSION).zip root@snorp.net:/var/www/files/dmap-sharp
