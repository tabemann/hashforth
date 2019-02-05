# Hashforth Architecture

Hashforth is a token-threaded Forth with four core variants, 64-bit, 32-bit-large, 32-bit-small, and 16-bit. It has two different kinds of token, 8/16-bit and 16/32-bit, with 64-bit and 32-bit-large having 16/32-bit tokens and 32-bit-small and 16-bit having 8/16-bit tokens. As should be apparent, 64-bit has 64-bit cells, 32-bit-large and 32-bit-small have 32-bit cells, and 16-bit has 16-bit cells. None of these variants support any special size for characters, and what are treated as characters in other Forths are simply treated as 8-bit bytes. It does not impose any alignment restrictions, and implementations will need to handle alignment on their own if the architectures they are implemented on have alignment restrictions. It also is little-endian, and implementations will need to convert endianness if the architectures they are implemented on are big-endian.

Tokens have two sizes for both 16/32-bit tokens and 8/16-bit tokens. In both of these cases by default tokens are the smaller size, and in that size can represent 15 and 7 bits respectively. To represent more tokens, specifically 2147516416
tokens and 32896 tokens, the first 16 or 8 bits has its high bit set to 1 (whereas otherwise it would be set to 0), which serves as a flag to indicate the existence of more bits to follow, and it is followed by another 16 or 8 bits which together represent the higher bits of the token plus 1.

## Primitives

There exist the following primitives in hashforth; for all these primitives, if not specified otherwise, the interpreter pointer after the word's execution points to the token immediately after the token executed:

### `HF_PRIM_END` (0), also known as `END`

#### ( -- )

This word should not be executed, and its execution indicates a bug in either the code being executed or the VM itself. Its execution results in an immediate exit.

### `HF_PRIM_NOP` (1), also known as `NOP`

#### ( -- )

This word is a no-op.

### `HF_PRIM_EXIT` (2), also known as `EXIT`

#### ( -- ) ( R: addr -- )

This word's execution results in the top of the return stack being popped and transferred to the interpreter pointer.

### `HF_PRIM_BRANCH` (3), also known as `BRANCH`

#### ( -- )

This word's execution results in the cell immediately after the token for this word being transferred to the interpreter pointer.

### `HF_PRIM_0BRANCH` (4), also known as `0BRANCH`

#### ( u -- )

This word's execution results in the value on the top of the data stack being popped; if its value is zero the cell immediately after the token for this word is transferred to the interpreter pointer; otherwise the interpreter pointer afterwards points to the token after the cell immediately after the token for this word.

### `HF_PRIM_LIT` (5), also known as `(LIT)`

#### ( -- n )

This word's execution results in the value in the cell immediately after the token for this word being pushed onto the top of the data stack, and the interpreter pointer points to the token after the cell immediately after the token for this word.

### `HF_PRIM_DATA` (6), also known as `(DATA)`

#### ( -- addr )

This word's execution results in the value in bytes in the cell immediately after the token for this word plus the size of one cell and the size of the token for this word being added to the interpreter pointer and the address of the byte immediately after the cell immediately after the token for this word being pushed onto the data stack.

### `HF_PRIM_NEW_COLON` (7), also known as `NEW-COLON`

#### ( addr -- xt )

This word's execution results in a new anonymous colon word being created, with the pointer to the start of its token-threaded code being popped off the top of the data stack, the data pointer being set to 0, and the token for the newly created word being pushed onto the data stack. This newly created word also has no next word.

### `HF_PRIM_NEW_CREATE` (8), also known as `NEW-CREATE`

#### ( addr -- xt )

This word's execution results in a new anonymous created word being created, with the pointer to the start of its data being popped off the top of the data stack, the TTC code pointer of that word being set to 0, and the token for the newly created word being pushed onto the data stack. This newly created word also has no next word. Created words when executed, by default, push the pointer to the start of their data onto the data stack.

### `HF_PRIM_SET_DOES` (9), also known as `SET-DOES>`

#### ( xt -- ) ( R: exit-addr does-addr -- )

This word's execution results in a token for a word being popped off the top of the data stack, an address being popped off the top of the return stack and being set to the TTC code pointer of that word, an address afterwards being popped off the top of the return stack and the interpreter pointer being set to it, and the word being set to being a `CREATE`/`DOES>` word which when executed pushes the pointer to the start of their data onto the data stack and then executes the token-threaded code at the TTC code pointer of the word.

### `HF_PRIM_FINISH` (10), also known as `FINISH`

#### ( xt -- )

This word's execution results in a token for a word being popped off the top of the data stack, and any necessary finalization of the word (other than the compilation of `EXIT` and `END`, which must be done before this is called) taking place, such as JIT compilation. In the current implementation, this is equivalent to `HF_PRIM_DROP`.

### `HF_PRIM_EXECUTE` (11), also known as `EXECUTE`

#### ( ? xt -- ? )

This word's execution results in a token for a word being popped off the top of the data stack, and that word being executed. If the word is not a primitive, the interpreter pointer as it would be after the execution of a normal word is pushed onto the return stack.

### `HF_PRIM_DROP` (12), also known as `DROP`

#### ( x -- )

This word's execution results in a cell being popped off the data stack and then being discarded.

### `HF_PRIM_DUP` (13), also known as `DUP`

#### ( x -- x x )

This word's execution results in the cell on the top of the data stack being duplicated as a new cell on the top of the data stack.

### `HF_PRIM_SWAP` (14), also known as `SWAP`

#### ( x1 x2 -- x2 x1 )

This word's execution results in the two cells on the top of the data stack exchanging places.

### `HF_PRIM_ROT` (15), also known as `ROT`

#### ( x1 x2 x3 -- x2 x3 x1 )

This word's execution results in third-from-the-top cell in the data stack being removed and being copied onto the top of the data stack, resulting in the second-from-the-top cell in the data stack now being in the third-from-the-top position and the cell on the top of the data stack now being in the second-from-the-top position.

### `HF_PRIM_PICK` (16), also known as `PICK`

#### ( xi ... x0 i -- xi ... x0 xi )

This word's execution results in an index being popped off the top of the data stack, afterwards which serves as a location relative to the top of the data stack, with zero being the top of the data stack after the index was removed, where the cell at that location is copied and placed on the top of the data stack.

### `HF_PRIM_ROLL` (17), also known as `ROLL`

#### ( xi xi-1 ... x0 i -- xi-1 ... x0 xi )

This word's execution results in an index being popped off the top of the data stack, afterwards which serves as a location relative to the top of the data stack, with zero being the top of the data stack after the index was removed, where the cell at that location is removed and placed on the top of the data stack.

### `HF_PRIM_LOAD` (18), also known as `@`

#### ( addr -- x )

This word's execution results in an address being popped off the top of the data stack, which is then dereferenced and the cell at that location is then pushed onto the data stack.

### `HF_PRIM_STORE` (19), also known as `!`

#### ( x addr -- )

This word's execution results in an address being popped off the top of the data stack, followed by a value being popped off the data stack from beneath it; afterwards, the address is dereferenced and the cell at that location is set to the value that was popped.

### `HF_PRIM_LOAD_8` (20), also known as `C@`

#### ( addr -- c )

This word's execution results in an address being popped off the top of the data stack, which is then dereferenced and the byte at that location is then pushed onto the data stack. Note that no sign extension takes place.

### `HF_PRIM_STORE_8` (21), also known as `C!`

#### ( c addr -- )

This word's execution results in an address being popped off the top of the data stack, followed by a value being popped off the data stack from beneath it; afterwards, the address is dereferenced and the byte at that location is set to the lower 8 bits of the value that was popped.

### `HF_PRIM_EQ` (22), also known as `=`

#### ( x1 x2 -- f )

This word's execution results in two values being popped off the top of the data stack, and if they are equal -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_NE` (23), also known as `<>`

#### ( x1 x2 -- f )

This word's execution results in two values being popped off the top of the data stack, and if they are not equal -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_LT` (24), also known as `<`

#### ( n1 n2 -- f )

This word's execution results in two signed values being popped off the top of the data stack, and if the lower value is (signed) smaller than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_GT` (25), also known as `>`

#### ( n1 n2 -- f )

This word's execution results in two signed values being popped off the top of the data stack, and if the lower value is (signed) greater than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_ULT` (26), also known as `U<`

#### ( u1 u2 -- f )

This word's execution results in two unsigned values being popped off the top of the data stack, and if the lower value is (unsigned) smaller than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_UGT` (27), also known as `U>`

#### ( u1 u2 -- f )

This word's execution results in two unsigned values being popped off the top of the data stack, and if the lower value is (unsigned) greater than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_NOT` (28), also known as `NOT`

#### ( u1 -- u2 )

This word's execution results in a value being popped off the top of the data stack and the same value with each bit being inverted being pushed back onto the data stack.

### `HF_PRIM_AND` (29), also known as `AND`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack and the bitwise and of these two values being subsequently pushed back onto the data stack.

### `HF_PRIM_OR` (30), also known as `OR`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack and the bitwise or of these two values being subsequently pushed back onto the data stack.

### `HF_PRIM_XOR` (31), also known as `XOR`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack and the bitwise exclusive or of these two values being subsequently pushed back onto the data stack.

### `HF_PRIM_LSHIFT` (32), also known as `LSHIFT`

#### ( x1 u2 -- x3 )

This word's execution results in two values being popped off the top of the data stack, where the lower value is shifted to the left by the number of bits indicated by the top-most value and then is pushed back onto the data stack.

### `HF_PRIM_RSHIFT` (33), also known as `RSHIFT`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack, where the lower value is logically (i.e. without sign extension) shifted to the right by the number of bits indicated by the top-most value and then is pushed back onto the data stack.

### `HF_PRIM_ARSHIFT` (34), also known as `ARSHIFT`

#### ( n1 u -- n2 )

This word's execution results in two values being popped off the top of the data stack, where the lower value is arithmetically (i.e. with sign extension) shifted to the right by the number of bits indicated by the top-most value and then is pushed back onto the data stack.

### `HF_PRIM_ADD` (35), also known as `+`

#### ( n1 n2 -- n3 )

This word's execution results in two values being popped off the top of the data stack, where then they undergo two's complement addition and the result is pushed back onto the data stack.

### `HF_PRIM_SUB` (36), also known as `-`

#### ( n1 n2 -- n3 )

This word's execution results in two values being popped off the top of the data stack, where then the top-most value undergoes two's complement subtraction from the lower value and the result is pushed back onto the data stack.

### `HF_PRIM_MUL` (37), also known as `*`

#### ( n1 n2 -- n3 )

This word's execution results in two values being popped off the top of the data stack, where then they undergo two's complement multiplication and the result is pushed back onto the data stack.

### `HF_PRIM_DIV` (38), also known as `/`

#### ( n1 n2 -- n3 )

This word's execution results in two values being popped off the top of the data stack, where then the lower value undergoes two's complement division by the top-most value and the result is pushed back onto the data stack.

### `HF_PRIM_MOD` (39), also known as `MOD`

#### ( n1 n2 -- n3 )

This word's execution results in two values being popped off the top of the data stack, where then the lower value undergoes two's complement modulus by the top-most value and the result is pushed back onto the data stack.

### `HF_PRIM_UDIV` (40), also known as `U/`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack, where then the lower value undergoes unsigned division by the top-most value and the result is pushed back onto the data stack.

### `HF_PRIM_UMOD` (41), also known as `UMOD`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack, where then the lower value undergoes unsigned modulus by the top-most value and the result is pushed back onto the data stack.

### `HF_PRIM_LOAD_R` (42), also known as `R@`

#### ( R: x -- x ) ( -- x )

This word's execution results in the top-most value on the return stack being pushed onto the data stack without being popped from the return stack.

### `HF_PRIM_PUSH_R` (43), also known as `>R`

#### ( x -- ) ( R: -- x )

This word's execution results in a value being popped off the top of the data stack and being pushed onto the return stack.

### `HF_PRIM_POP_4` (44), also known as `R>`

#### ( R: x -- ) ( -- x )

This word's execution results in a value being popped off the top of the return stack and being pushed onto the data stack.

### `HF_PRIM_LOAD_SP` (45), also known as `SP@`

#### ( -- addr )

This word's execution results in the data stack pointer prior to the execution of this word being pushed onto the data stack.

### `HF_PRIM_STORE_SP` (46), also known as `SP!`

#### ( addr -- )

This word's execution results in the data stack pointer being set to an address popped off the top of the data stack.

### `HF_PRIM_LOAD_RP` (47), also known as `RP@`

#### ( -- addr )

This word's execution results in the return stack pointer being pushed onto the data stack.

### `HF_PRIM_STORE_RP` (48), also known as `RP!`

#### ( addr -- )

This word's execution results in the return stack pointer being set to an address popped off the top of the data stack.

### `HF_PRIM_TO_BODY` (49), also known as `>BODY`

#### ( xt -- addr )

This word's execution results in a token being popped off the top of the data stack and the data pointer for the word referred to by the token being subsequently pushed onto the data stack.

### `HF_PRIM_WORD_TO_NAME` (50), also known as `WORD>NAME`

#### ( xt -- addr u )

This word's execution results in a token being popped off the top of the data stack, where then a pointer to the beginning of the name set for the word referred to by the token is pushed onto the data stack followed by the length in bytes of the name set for that word being pushed onto the data stack.

### `HF_PRIM_NAME_TO_WORD` (51), also known as `NAME>WORD`

#### ( addr u xt -- )

This word's execution results in a token being popped off the top of the data stack followed by a name length and a pointer to the beginning of a name being popped off the top of the data stack in that order, where then the name of the word referred to by that token is set to the name pointer and name length popped off the data stack.

### `HF_PRIM_WORD_TO_NEXT` (52), also known as `WORD>NEXT`

#### ( xt1 -- xt2|0 )

This word's execution results in a token being popped off the top of the data stack, where then the next value stored in the word referred to by the token, typically the token of another word or 0, is pushed onto the data stack.

### `HF_PRIM_NEXT_TO_WORD` (53), also known as `NEXT>WORD`

#### ( xt1|0 xt2 -- )

This word's execution results in a token being popped off the top of the data stack followed by another token or 0 being popped off the top of the data stack, where then the next value stored in the word referred to by the top-most token is set to the second value.

### `HF_PRIM_WORD_TO_FLAGS` (54), also known as `WORD>FLAGS`

#### ( xt -- flags )

This word's execution results in a token being popped off the top of the data stack, where then the flags value stored in the word referred to by the token is pushed onto the data stack.

### `HF_PRIM_FLAGS_TO_WORD` (55), also known as `FLAGS>WORD`

#### ( flags xt -- )

This word's execution results in a token being popped off the top of the data stack followed by a flags value, where then the flags value stored in the word referred to by the token is set to the flags value popped off the data stack.

### `HF_PRIM_LOAD_16` (56), also known as `H@`

#### ( addr -- h )

This word's execution results in an address being popped off the top of the data stack, which is then dereferenced and the 16-bit value at that location is then pushed onto the data stack. Note that no sign extension takes place.

### `HF_PRIM_STORE_16` (57), also known as `H!`

#### ( h addr -- )

This word's execution results in an address being popped off the top of the data stack, followed by a value being popped off the data stack from beneath it; afterwards, the address is dereferenced and the 16-bit value at that location is set to the lower 16 bits of the value that was popped.

### `HF_PRIM_LOAD_32` (58), also known as `W@`

#### ( addr -- w )

This word's execution results in an address being popped off the top of the data stack, which is then dereferenced and the 32-bit value at that location is then pushed onto the data stack. Note that no sign extension takes place.

### `HF_PRIM_STORE_32` (59), also known as `W!`

#### ( w addr -- )

This word's execution results in an address being popped off the top of the data stack, followed by a value being popped off the data stack from beneath it; afterwards, the address is dereferenced and the 32-bit value at that location is set to the lower 32 bits of the value that was popped.

### `HF_PRIM_SET_WORD_COUNT` (60), also known as `SET-WORD-COUNT`

#### ( u -- )

This word's execution results in the number of words in the word table being set to a count popped off the top of the data stack; this count must be less than or equal to the number of words in the word table.

### `HF_PRIM_SYS` (61), also known as `SYS`

#### ( ? n -- ? f )

This word's execution results in a service number being popped off the top of the data stack, which is non-negative for standard services and negative for non-standard services, which if corresponding to an implemented service results in the parameters for that service being popped off the top of the data stack and the results being pushed onto the data stack followed by -1 being pushed onto the data stack, and otherwise 0 is pushed onto the data stack.

## Services

Services are invoked with the `HF_PRIM_SYS` (`SYS`) word; in the following documentation the service numbers used to invoke the services and the flags returned to indicate that said services exist are not included in the stack signatures for these services.

All these services are standard services, and hence have non-negative service numbers. Non-standard services have negative service numbers.

### `HF_SYS_UNDEFINED` (0)

This service is never defined.

### `HF_SYS_LOOKUP` (1)

#### ( addr u -- n )

Look up a service number by name, as specified by a name length popped off the top of the data stack followed by a pointer to the start of the name; if the service exists its service number is pushed onto the data stack, otherwise 0, i.e. `HF_SYS_UNDEFINED`, is pushed onto the data stack.

### `HF_SYS_BYE` (2)

#### ( -- )

Exit the Forth runtiime.

### `HF_SYS_ALLOCATE` (3)

#### ( u -- addr f )

Allocate memory on the heap, as specified by a number of bytes popped off the top of the data stack, and push the pointer to the allocated memory onto the data stack followed by -1 indicating success (in the current implementation this word is always successful, or otherwise it exits abnormally).

### `HF_SYS_RESIZE` (4)

#### ( addr1 u -- addr2 f )

Resize a block of memory allocated on the heap with `HF_SYS_ALLOCATE` or `HF_SYS_RESIZE`, as specified by a new number of bytes popped off the top of the data stack followed by the address of an existing allocated block of memory, and push the pointer to the resized memory, which may or may not change starting address (with the data included in the original block of memory being copied if necessary), being pushed onto the data stack followed by -1 indicating success (in the current implementation this word is always successful, or otherwise it exits abnormally).

### `HF_SYS_FREE` (5)

#### ( addr -- )

Free a block of memory allocated on the heap with `HF_SYS_ALLOCATE` or `HF_SYS_RESIZE`, as specified by an address of an existing allocated block of memory popped off the top of the data stack, and push -1 onto the data stack indicating success (in the current implementation this word is always successful, or otherwise it exits abnormally).

### `HF_SYS_OPEN` (6)

#### ( addr u flags mode -- fd f )

Open a file with the specified flags and mode, as specified by a mode popped off the top of the data stack, followed by flags, and then name length and a pointer to the start of the name. The mode is an OS-specific value which may indicate permissions for newly created files. The flags are one of `HF_OPEN_RDONLY` (1), indicating to open the file in read-only mode, `HF_OPEN_WRONLY` (2), indicating to open the file in write-only mode, or `HF_OPEN_RDWR` (4), indicating to open the file in read-write mode, which if `HF_OPEN_WRONLY` is set, may be or'ed with `HF_OPEN_APPEND` (8) to put the file in appending mode, and if either `HF_OPEN_WRONLY` or `HF_OPEN_RDWR` is set, may be or'ed with `HF_OPEN_CREAT` (16) to specify that a file may be created if it does not already exist, with `HF_OPEN_EXCL` (32) combined with `HF_OPEN_CREAT` to specify that a file *must* be created or else opening will fail, and/or with `HF_OPEN_TRUNC` (64) to specify that if the file already exists and is a regular file the file will be truncated to length 0. If opening the file is successful the file descriptor followed by -1 are pushed onto the data stack, otherwise 0 followed by 0 are pushed onto the data stack.

### `HF_SYS_CLOSE` (7)

#### ( fd -- f )

Close a file, as specified by a file descriptor popped off the top of the data stack, and push -1 onto the data stack if successful, or otherwise push 0 onto the data stack.

### `HF_SYS_READ` (8)

#### ( addr u fd -- u f )

Read from a file descriptor, as specified by a file descriptor popped off the top of the data stack, followed by the maximum number of bytes to read and then a pointer to the start of a buffer into which bytes are to be read. The total number of bytes read, 0 in the case of a closed file descriptor, an error, or a non-blocking file descriptor that would block, is pushed onto the data stack, followed by -1 for a successful read or a closed file descriptor, 1 for a non-blocking file descriptor that would block, or 0 for an error.

### `HF_SYS_WRITE` (9)

#### ( addr u fd -- u f )

Write to a file descriptor, as specified by a file descriptor popped off the top of the data stack, followed by the maximum number of bytes to write and then a pointer to the start of a buffer from which bytes are to be written. The total number of bytes written is pushed onto the data stack, followed by -1 for a successful write, 1 for a non-blocking file descriptor that would block, or 0 for an error.

### `HF_SYS_GET_NONBLOCKING` (10)

#### ( fd -- f1 f2 )

Get whether a file descriptor is non-blocking, as specified by a file descriptor popped off the top of the data stack. -1 is pushed onto the data stack if the file descriptor is non-blocking, otherwise 0 is pushed onto the data stack; afterwards, -1 is pushed onto the data stack if the operation was successful, otherwise 0 is pushed onto the data stack.

### `HF_SYS_SET_NONBLOCKING` (11)

#### ( f1 fd -- f2 )

Set whether a file descriptor is non-blocking, as specified by a file descriptor popped off the top of the data stack followed by a flag indicating whether the file descriptor is to be non-blocking (non-zero values result in the file descriptor being set to be non-blocking and zero results in the file descriptor being set to be blocking). -1 is pushed onto the data stack if the operation was successful, otherwise 0 is pushed onto the data stack.

### `HF_SYS_ISATTY` (12)

#### ( fd -- f1 f2 )

Get whether a file descriptor corresponds to a terminal, as specified by a file descriptor popped off the top of the data stack. -1 is pushed onto the data stack if the file descriptor corresponds to a terminal, otherwise 0 is pushed onto the data stack; afterwards, -1 is pushed onto the data stack if the operation was successful, otherwise 0 is pushed onto the data stack.

### `HF_SYS_POLL` (13)

#### ( addr u1 ms -- u2 f )

Wait for IO or a timeout, as specified by a number of milliseconds to wait (-1 to wait forever) popped off the top of the data stack, followed by a number of file descriptor entries to wait on and then a pointer to a data structure containing the information indicating what file descriptors to wait on, what events to wait for, and what events occurred. The data structure consists of `u1` entries, each consisting of three cells, the first cell being a file descriptor, the second cell being any of `HF_POLL_IN` (1), indicating to wait for input, `HF_POLL_OUT` (2), indicating to wait for output, and/or `HF_POLL_PRI` (4), indicating to wait for priority input, or'ed together, and the third cell receiving any of `HF_POLL_IN`, indicating that input occurred, `HF_POLL_OUT`, indicating that output occurred, `HF_POLL_PRI`, indicating that priority input occurred, `HF_POLL_ERR` (8), indicating that an error occurred, `HF_POLL_HUP` (16), indicating that a hang-up occurred, or `HF_POLL_NVAL` (32), indicating that invalid input was provided, or'ed together. Afterwards, the number of file descriptors for which events occurred is pushed onto the data stack, followed by -1 for success and 0 for failure being pushed onto the data stack.

### `HF_SYS_GET_MONOTONIC_TIME` (14)

#### ( -- s ns )

Get a monotonic time, i.e. a time that is guaranteed to not have jumps, pushing the current number of seconds onto the data stack followed by pushing the current number of nanoseconds onto the data stack.

### `HF_SYS_GET_TRACE` (15)

#### ( -- f )

Get whether a debugging tracer is active, pushing -1 onto the data stack if it is active, and otherwise pushing 0 onto the data stack.

### `HF_SYS_SET_TRACE` (16)

#### ( f -- )

Set whether a debugging tracer is active, popping a flag off the top of the data stack, turning on debugging tracing if it is non-zero and the runtime is compiled to support debugging tracing, and otherwise turning off debugging tracing.

### `HF_SYS_GET_SBASE` (17)

#### ( -- addr )

Get the data stack base used by a debugging tracer, pushing it onto the data stack.

### `HF_SYS_SET_SBASE` (18)

#### ( addr -- )

Set the data stack base used by a debugging tracer, popping it off the top of the data stack.

### `HF_SYS_GET_RBASE` (19)

#### ( -- addr )

Get the return stack base used by a debugging tracer, pushing it onto the data stack.

### `HF_SYS_SET_RBASE` (20)

#### ( addr -- )

Set the return stack base used by a debugging tracer, popping it off the top of the data stack.