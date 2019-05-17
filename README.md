# hashforth

Hashforth is a #forth proof of concept implementation. It has two components, the hashforth executable, which is written in portable ANSI C, and the hashforth image, which is assembled with an assembler written in Forth and which is loaded and executed by the hashforth executable.

Building the hashforth executable is carried out by executing the following at a shell prompt at the base directory of the hashforth tree:

    $ make

or:

    $ make CFLAGS="<flags>"

This builds hashforth in the current directory. Note that \<flags> may contain one of `-DCELL_16`, `-DCELL_32`, or `-DCELL_64` (defaulting to `-DCELL_64`), which sets the cell size it is compiled for (note that this is architecture-specific), and one of `-DTOKEN_8_16`, `-DTOKEN_16`, `-DTOKEN_16_32`, or `-DTOKEN_32` (defaulting to `-DTOKEN_32`), which sets the token size (with `-DTOKEN_8_16` having either 8 or 16-bit tokens and `-DTOKEN_16_32` having either 16 or 32-bit tokens). There are also the flags `-DTRACE`, which compiles in tracing executed words, and `-DSTACK_TRACE` which, when combined with `-DTRACE`, outputs the contents of the data stack for each word traced. Omitting `CFLAGS="<flags>"` is equivalent to specifying `CFLAGS="-O2 -DCELL_64 -DTOKEN_16_32"`.

Executing hashforth is carried out by executing the following at a shell prompt, e.g. at the base directory of the hashforth tree:

    $ ./hashforth <image>

Here \<image> is the path of a hashforth image. An image for 64-bit systems with 16-bit/32-bit tokens, i.e. for a hashforth executable compiled with `CFLAGS="-DCELL_64 -DTOKEN_16_32`, is included at `images/cell_64_token_16_32.image`. After hashforth loads, the user can then enter Forth code to be interpretd at the terminal.

Assembling a new hashforth image is carried out by, once hashforth is started, entering the following:

    include src/asm/build_image.fs

By default this will build a new hashforth image at `images/cell_64_token_16_32.image`, which will be compiled for 64-bit cells and 16/32-bit tokens. Note that the configuration of this image must match the configuration the hashforth executable is compiled for. To change the configuration of the image, on the line containing `INIT-ASM` in `src/asm/asm.fs`, specify `CELL-16` or `CELL-32` instead of `CELL-64`, to change the cell size, or specify `TOKEN-8-16`, `TOKEN-16`, or `TOKEN-32` instead of `TOKEN-16-32`, to change the token size. After changing either of these settings, it is recommended to change the filename in the line containing `WRITE-ASM-TO-FILE` in `src/asm/build_image.fs` to reflect the changed configuration. Note that the hashforth assembler may only assembler images with a cell size smaller than or equal to the cell size for which hashforth is compiled (i.e. the address word size on the target machine),