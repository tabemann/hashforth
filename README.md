# hashforth

Hashforth is a #forth proof of concept implementation. It has two components, the hashforth executable, which is written in portable ANSI C, and the hashforth image, which is assembled with an assembler written in Forth and which is loaded and executed by the hashforth executable.

Building the hashforth executable is carried out by executing the following at a shell prompt at the base directory of the hashforth tree:

    $ make CFLAGS="<flags>"

This builds hashforth in the current directory. Note that \<flags> may contain one of `-DCELL_16`, `-DCELL_32`, or `-DCELL_64` (defaulting to `-DCELL_64`), which sets the cell size it is compiled for (note that this is architecture-specific), and one of `-DTOKEN_8_16`, `-DTOKEN_16`, `-DTOKEN_16_32`, or `-DTOKEN_32` (defaulting to `-DTOKEN_32`), which sets the token size (with `-DTOKEN_8_16` having either 8 or 16-bit tokens and `-DTOKEN_16_32` having either 16 or 32-bit tokens). There are also the flags `-DTRACE`, which compiles in tracing executed words, and `-DSTACK_TRACE` which, when combined with `-DTRACE`, outputs the contents of the data stack for each word traced. Note, however, that `-DSTACK_TRACE` is not necessarily compatible with code that manipulates the manually manipulates the data stack pointer beyond simple exception handling, such as code that involves multitasking.

Assembling the hashforth image is carried out with a different Forth implementation, attoforth, which is at https://github.com/tabemann/attoforth . Once it is built and installed, the hashforth image is assembled by executing the following at a shell prompt at the base directory of the hashforth tree:

    $ attoforth src/asm/build_image.fs

By default this will build a new hashforth image at `images/cell_64_token_16_32.image`, which will be compiled for 64-bit cells and 16/32-bit tokens. Note that the configuration of this image must match the configuration the hashforth executable is compiled for. To change the configuration of the image, on the line containing `INIT-ASM` in `src/asm/build_image.fs`, specify `CELL-16` or `CELL-32` instead of `CELL-64`, to change the cell size, or specify `TOKEN-8-16`, `TOKEN-16`, or `TOKEN-32` instead of `TOKEN-16-32`, to change the token size. After changing either of these settings, it is recommended to change the filename in the line containing `WRITE-ASM-TO-FILE` to reflect the changed configuration.

To avoid needing to download and install attoforth, a pre-assembled copy of `images/cell_64_token_16_32.image` is in this git repository; however, it is not necessarily guaranteed to reflect the latest code.