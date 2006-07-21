SUBDIRS = src sample

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = daap-sharp.pc

release: dist
	svn cp svn+ssh://jwilcox@mono-cvs.ximian.com/source/trunk/daap-sharp \
        svn+ssh://jwilcox@mono-cvs.ximian.com/source/tags/daap-sharp-$(VERSION) -m "tag for $(VERSION)"
	scp daap-sharp-$(VERSION).tar.gz root@snorp.net:/var/www/files/daap-sharp

winbuild: all
	make distclean
	./autogen.sh --enable-mdnsd
	make
	make dist
	rm -rf daap-sharp-$(VERSION)
	rm -f daap-sharp-$(VERSION).zip
	tar xvfz daap-sharp-$(VERSION).tar.gz
	zip -r daap-sharp-$(VERSION).zip daap-sharp-$(VERSION)
	rm -rf daap-sharp-$(VERSION)
	mkdir daap-sharp-$(VERSION)
	cp src/daap-sharp.dll daap-sharp-$(VERSION)
	cp sample/*.exe daap-sharp-$(VERSION)
	cp sample/entagged-sharp.dll daap-sharp-$(VERSION)
	cp `pkg-config --variable=libdir mono`/mono/1.0/ICSharpCode.SharpZipLib.dll daap-sharp-$(VERSION)
	zip -r daap-sharp-$(VERSION)-binary.zip daap-sharp-$(VERSION)
	rm -rf daap-sharp-$(VERSION)
	make distclean

winrelease: winbuild
	scp daap-sharp-$(VERSION)-binary.zip root@snorp.net:/var/www/files/daap-sharp
	scp daap-sharp-$(VERSION).zip root@snorp.net:/var/www/files/daap-sharp
