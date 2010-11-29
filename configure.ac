AC_INIT(README)
AC_CANONICAL_SYSTEM

MAJOR_VERSION=0
MINOR_VERSION=4
MICRO_VERSION=0
VERSION=$MAJOR_VERSION.$MINOR_VERSION.$MICRO_VERSION
                                                                                
AM_INIT_AUTOMAKE(dmap-sharp, $VERSION)
                                                                                
AC_SUBST(MAJOR_VERSION)
AC_SUBST(MINOR_VERSION)
AC_SUBST(MICRO_VERSION)
AC_SUBST(VERSION)
                                                                                
AC_DEFINE_UNQUOTED(MAJOR_VERSION, $MAJOR_VERSION, [Major Version Number])
AC_DEFINE_UNQUOTED(MINOR_VERSION, $MINOR_VERSION, [Minor Version Number])
AC_DEFINE_UNQUOTED(MICRO_VERSION, $MICRO_VERSION, [Micro Version Number])

AM_MAINTAINER_MODE

AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi

PKG_CHECK_MODULES(MZC, mono-zeroconf >= 0.7.1)
MCS_FLAGS=$MZC_LIBS
AC_SUBST(MCS_FLAGS)

dnl Mono	   
AC_PATH_PROG(MONO, mono)
if test "x$MONO" = "x" ; then
   AC_MSG_ERROR([Can not find "mono" in your PATH])
fi

dnl mcs	   
AC_PATH_PROG(MCS, gmcs)
if test "x$MCS" = "x" ; then
   AC_MSG_ERROR([Can not find "gmcs" in your PATH])
fi

PKG_CHECK_MODULES(TAGLIB, taglib-sharp >= 2.0.2, have_taglib=yes, have_taglib=no)
PKG_CHECK_MODULES(MONODOC, monodoc >= 1.1.9, have_monodoc=yes, have_monodoc=no)

AM_CONDITIONAL(HAVE_TAGLIB, test "x$have_taglib" = "xyes")

if test "x$have_monodoc" = "xyes"; then
    AC_PATH_PROG(MONODOCER, monodocer)
    AC_PATH_PROG(MDASSEMBLER, mdassembler)

    MONODOC_DIR=`$PKG_CONFIG --variable=sourcesdir monodoc`
    AC_SUBST(MONODOC_DIR)
fi
#AM_CONDITIONAL(HAVE_MONODOC, test "x$have_monodoc" = "xyes")
AM_CONDITIONAL(HAVE_MONODOC, test "x$have_monodoc" = "xnever")

AC_PATH_PROG(GACUTIL, gacutil, no)
if test "x$GACUTIL" = "xno"; then
  AC_MSG_ERROR([You need to install gacutil])
fi


AC_OUTPUT([
Makefile
dmap-sharp.pc
src/Makefile
src/dmap-sharp.dll.config
sample/Makefile
])

echo "
    Installation Prefix:     $prefix
    C# Compiler:             $MCS

    Developer Documentation: $have_monodoc
"
