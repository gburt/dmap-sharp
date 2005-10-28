SUBDIRS = src sample

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = daap-sharp.pc

release: dist
	svn cp svn+ssh://jwilcox@mono-cvs.ximian.com/source/trunk/daap-sharp \
        svn+ssh://jwilcox@mono-cvs.ximian.com/source/tags/daap-sharp-$(VERSION) -m "tag for $(VERSION)"
	scp daap-sharp-$(VERSION).tar.gz root@snorp.net:/var/www/html/files/daap-sharp

