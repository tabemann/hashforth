# Hashforth Architecture

Hashforth is a token-threaded Forth with four core variants, 64-bit, 32-bit-large, 32-bit-small, and 16-bit. It has two different kinds of token, 8/16-bit and 16/32-bit, with 64-bit and 32-bit-large having 16/32-bit tokens and 32-bit-small and 16-bit having 8/16-bit tokens. As should be apparent, 64-bit has 64-bit cells, 32-bit-large and 32-bit-small have 32-bit cells, and 16-bit has 16-bit cells. None of these variants support any special size for characters, and what are treated as characters in other Forths are simply treated as 8-bit bytes. It does not impose any alignment restrictions, and implementations will need to handle alignment on their own if the architectures they are implemented on have alignment restrictions. It also is little-endian, and implementations will need to convert endianness if the architectures they are implemented on are big-endian.

Tokens have two sizes for both 16/32-bit tokens and 8/16-bit tokens. In both of these cases by default tokens are the smaller size, and in that size can represent 15 and 7 bits respectively. To represent more tokens, specifically 2147516416
tokens and 32896 tokens, the first 16 or 8 bits has its high bit set to 1 (whereas otherwise it would be set to 0), which serves as a flag to indicate the existence of more bits to follow, and it is followed by another 16 or 8 bits which together represent the higher bits of the token plus 1.

## Images

Images have the following basic format:

 * (1 byte) Cell type (0 for 16-bit cells, 1 for 32-bit cells, and 2 for 64-bit cells)
 * (1 byte) Token type (0 for 8/16-bit tokens, 1 for 16-bit tokens, 2 for 16/32-bit tokens, and 3 for 32-bit tokens; note that only 8/16-bit and 16/32-bit tokens are standard)
 * (1 cell) Total memory size in bytes, aside from memory allocated with `HF_SYS_ALLOCATE`
 * (1 cell) Maximum word count in number of words
 * (1 cell) Return stack size in number of cells
 * Word headers
 * (1 cell) Size of data to copy into user space in bytes
 * Data to copy into user space
 * Stored data to copy anywhere in memory

Word headers have the following format:

 * (1 byte) Header type (0 for end of headers, 1 for colon words, 2 for `CREATE` words); if this is 0 no more data is present in the headers after this byte
 * (1 cell) Token for word; note that tokens must be in order from lowest to highest with no gaps, including between the first token in the image and the last token for a primitive
 * (1 cell) Word offset from start of image data copied into user space; for colon words this corresponds to the beginning of token-threaded code for the word and for `CREATE` words this corresponds to the data pointer of the word

Once data for words is loaded, the words are relocated such that token-threaded code pointers and data pointers point at the specified offset plus the address of the start of the data loaded in memory. Within each colon word loaded, each `HF_PRIM_BRANCH` and `HF_PRIM_0BRANCH` has the offset specified for it added to said address. Note that `HF_PRIM_END` is used to specify that no more tokens are present in a given word, and thus each loaded word must end in this token.

The stored data is copied into an implementation-dependent location in memory.

After all loading is complete, the stored data address, the stored data length, and the current user space address are pushed onto the data stack and the last word loaded is executed.

## Primitives

There exist the following primitives in hashforth; for all these primitives, if not specified otherwise, the interpreter pointer after the word's execution points to the token immediately after the token executed:

### `HF_PRIM_END` (0)

#### ( -- )

This word should not be executed, and its execution indicates a bug in either the code being executed or the VM itself. Its execution results in an immediate exit.

### `HF_PRIM_NOP` (1), also known as `nop`

#### ( -- )

This word is a no-op.

### `HF_PRIM_EXIT` (2), also known as `exit`

#### ( -- ) ( R: addr -- )

This word's execution results in the top of the return stack being popped and transferred to the interpreter pointer.

### `HF_PRIM_BRANCH` (3), also known as `branch`

#### ( -- )

This word's execution results in the cell immediately after the token for this word being transferred to the interpreter pointer.

### `HF_PRIM_0BRANCH` (4), also known as `0branch`

#### ( u -- )

This word's execution results in the value on the top of the data stack being popped; if its value is zero the cell immediately after the token for this word is transferred to the interpreter pointer; otherwise the interpreter pointer afterwards points to the token after the cell immediately after the token for this word.

### `HF_PRIM_LIT` (5), also known as `(lit)`

#### ( -- n )

This word's execution results in the value in the cell immediately after the token for this word being pushed onto the top of the data stack, and the interpreter pointer points to the token after the cell immediately after the token for this word.

### `HF_PRIM_LIT_8` (6), also known as `(litc)`

#### ( -- n )

This word's execution results in the value in the byte immediately after the token for this word being pushed onto the top of the data stack with sign extension, and the interpreter pointer points to the token after the byte immediately after the token for this word.

### `HF_PRIM_LIT_16` (7), also known as `(lith)`

#### ( -- n )

This word's execution results in the value in the 16 bits immediately after the token for this word being pushed onto the top of the data stack with sign extension, and the interpreter pointer points to the token after the 16 bits immediately after the token for this word.

### `HF_PRIM_LIT_32` (8), also known as `(litw)`

#### ( -- n )

This word's execution results in the value in the 32 bits immediately after the token for this word being pushed onto the top of the data stack with sign extension, and the interpreter pointer points to the token after the 32 bits immediately after the token for this word.

### `HF_PRIM_DATA` (9), also known as `(data)`

#### ( -- addr )

This word's execution results in the value in bytes in the cell immediately after the token for this word plus the size of one cell and the size of the token for this word being added to the interpreter pointer and the address of the byte immediately after the cell immediately after the token for this word being pushed onto the data stack.

### `HF_PRIM_NEW_COLON` (10), also known as `new-colon`

#### ( addr -- xt )

This word's execution results in a new anonymous colon word being created, with the pointer to the start of its token-threaded code being popped off the top of the data stack, the data pointer being set to 0, and the token for the newly created word being pushed onto the data stack. This newly created word also has no next word.

### `HF_PRIM_NEW_CREATE` (11), also known as `new-create`

#### ( addr -- xt )

This word's execution results in a new anonymous created word being created, with the pointer to the start of its data being popped off the top of the data stack, the TTC code pointer of that word being set to 0, and the token for the newly created word being pushed onto the data stack. This newly created word also has no next word. Created words when executed, by default, push the pointer to the start of their data onto the data stack.

### `HF_PRIM_SET_DOES` (12), also known as `set-does>`

#### ( xt -- ) ( R: exit-addr does-addr -- )

This word's execution results in a token for a word being popped off the top of the data stack, an address being popped off the top of the return stack and being set to the TTC code pointer of that word, an address afterwards being popped off the top of the return stack and the interpreter pointer being set to it, and the word being set to being a `CREATE`/`DOES>` word which when executed pushes the pointer to the start of their data onto the data stack and then executes the token-threaded code at the TTC code pointer of the word.

### `HF_PRIM_FINISH` (13), also known as `finish`

#### ( xt -- )

This word's execution results in a token for a word being popped off the top of the data stack, and any necessary finalization of the word (other than the compilation of `EXIT` and `END`, which must be done before this is called) taking place, such as JIT compilation. In the current implementation, this is equivalent to `HF_PRIM_DROP`.

### `HF_PRIM_EXECUTE` (14), also known as `execute`

#### ( ? xt -- ? )

This word's execution results in a token for a word being popped off the top of the data stack, and that word being executed. If the word is not a primitive, the interpreter pointer as it would be after the execution of a normal word is pushed onto the return stack.

### `HF_PRIM_DROP` (15), also known as `drop`

#### ( x -- )

This word's execution results in a cell being popped off the data stack and then being discarded.

### `HF_PRIM_DUP` (16), also known as `dup`

#### ( x -- x x )

This word's execution results in the cell on the top of the data stack being duplicated as a new cell on the top of the data stack.

### `HF_PRIM_SWAP` (17), also known as `swap`

#### ( x1 x2 -- x2 x1 )

This word's execution results in the two cells on the top of the data stack exchanging places.

### `HF_PRIM_OVER` (18), also known as `over`

#### ( x1 x2 -- x1 x2 x1 )

This word's execution results in the cell beneath the top of the data stack being pushed onto the top of the data stack.

### `HF_PRIM_ROT` (19), also known as `rot`

#### ( x1 x2 x3 -- x2 x3 x1 )

This word's execution results in third-from-the-top cell in the data stack being removed and being copied onto the top of the data stack, resulting in the second-from-the-top cell in the data stack now being in the third-from-the-top position and the cell on the top of the data stack now being in the second-from-the-top position.

### `HF_PRIM_PICK` (20), also known as `pick`

#### ( xi ... x0 i -- xi ... x0 xi )

This word's execution results in an index being popped off the top of the data stack, afterwards which serves as a location relative to the top of the data stack, with zero being the top of the data stack after the index was removed, where the cell at that location is copied and placed on the top of the data stack.

### `HF_PRIM_ROLL` (21), also known as `roll`

#### ( xi xi-1 ... x0 i -- xi-1 ... x0 xi )

This word's execution results in an index being popped off the top of the data stack, afterwards which serves as a location relative to the top of the data stack, with zero being the top of the data stack after the index was removed, where the cell at that location is removed and placed on the top of the data stack.

### `HF_PRIM_LOAD` (22), also known as `@`

#### ( addr -- x )

This word's execution results in an address being popped off the top of the data stack, which is then dereferenced and the cell at that location is then pushed onto the data stack.

### `HF_PRIM_STORE` (23), also known as `!`

#### ( x addr -- )

This word's execution results in an address being popped off the top of the data stack, followed by a value being popped off the data stack from beneath it; afterwards, the address is dereferenced and the cell at that location is set to the value that was popped.

### `HF_PRIM_LOAD_8` (24), also known as `c@`

#### ( addr -- c )

This word's execution results in an address being popped off the top of the data stack, which is then dereferenced and the byte at that location is then pushed onto the data stack. Note that no sign extension takes place.

### `HF_PRIM_STORE_8` (25), also known as `c!`

#### ( c addr -- )

This word's execution results in an address being popped off the top of the data stack, followed by a value being popped off the data stack from beneath it; afterwards, the address is dereferenced and the byte at that location is set to the lower 8 bits of the value that was popped.

### `HF_PRIM_EQ` (26), also known as `=`

#### ( x1 x2 -- f )

This word's execution results in two values being popped off the top of the data stack, and if they are equal -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_NE` (27), also known as `<>`

#### ( x1 x2 -- f )

This word's execution results in two values being popped off the top of the data stack, and if they are not equal -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_LT` (28), also known as `<`

#### ( n1 n2 -- f )

This word's execution results in two signed values being popped off the top of the data stack, and if the lower value is (signed) smaller than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_GT` (29), also known as `>`

#### ( n1 n2 -- f )

This word's execution results in two signed values being popped off the top of the data stack, and if the lower value is (signed) greater than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_ULT` (30), also known as `u<`

#### ( u1 u2 -- f )

This word's execution results in two unsigned values being popped off the top of the data stack, and if the lower value is (unsigned) smaller than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_UGT` (31), also known as `u>`

#### ( u1 u2 -- f )

This word's execution results in two unsigned values being popped off the top of the data stack, and if the lower value is (unsigned) greater than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### `HF_PRIM_NOT` (32), also known as `not`

#### ( u1 -- u2 )

This word's execution results in a value being popped off the top of the data stack and the same value with each bit being inverted being pushed back onto the data stack.

### `HF_PRIM_AND` (33), also known as `and`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack and the bitwise and of these two values being subsequently pushed back onto the data stack.

### `HF_PRIM_OR` (34), also known as `or`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack and the bitwise or of these two values being subsequently pushed back onto the data stack.

### `HF_PRIM_XOR` (35), also known as `xor`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack and the bitwise exclusive or of these two values being subsequently pushed back onto the data stack.

### `HF_PRIM_LSHIFT` (36), also known as `lshift`

#### ( x1 u2 -- x3 )

This word's execution results in two values being popped off the top of the data stack, where the lower value is shifted to the left by the number of bits indicated by the top-most value and then is pushed back onto the data stack.

### `HF_PRIM_RSHIFT` (37), also known as `rshift`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack, where the lower value is logically (i.e. without sign extension) shifted to the right by the number of bits indicated by the top-most value and then is pushed back onto the data stack.

### `HF_PRIM_ARSHIFT` (38), also known as `arshift`

#### ( n1 u -- n2 )

This word's execution results in two values being popped off the top of the data stack, where the lower value is arithmetically (i.e. with sign extension) shifted to the right by the number of bits indicated by the top-most value and then is pushed back onto the data stack.

### `HF_PRIM_ADD` (39), also known as `+`

#### ( n1 n2 -- n3 )

This word's execution results in two values being popped off the top of the data stack, where then they undergo two's complement addition and the result is pushed back onto the data stack.

### `HF_PRIM_SUB` (40), also known as `-`

#### ( n1 n2 -- n3 )

This word's execution results in two values being popped off the top of the data stack, where then the top-most value undergoes two's complement subtraction from the lower value and the result is pushed back onto the data stack.

### `HF_PRIM_MUL` (41), also known as `*`

#### ( n1 n2 -- n3 )

This word's execution results in two values being popped off the top of the data stack, where then they undergo two's complement multiplication and the result is pushed back onto the data stack.

### `HF_PRIM_DIV` (42), also known as `/`

#### ( n1 n2 -- n3 )

This word's execution results in two values being popped off the top of the data stack, where then the lower value undergoes two's complement division by the top-most value and the result is pushed back onto the data stack.

### `HF_PRIM_MOD` (43), also known as `mod`

#### ( n1 n2 -- n3 )

This word's execution results in two values being popped off the top of the data stack, where then the lower value undergoes two's complement modulus by the top-most value and the result is pushed back onto the data stack.

### `HF_PRIM_UDIV` (44), also known as `u/`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack, where then the lower value undergoes unsigned division by the top-most value and the result is pushed back onto the data stack.

### `HF_PRIM_UMOD` (45), also known as `umod`

#### ( u1 u2 -- u3 )

This word's execution results in two values being popped off the top of the data stack, where then the lower value undergoes unsigned modulus by the top-most value and the result is pushed back onto the data stack.

### `HF_PRIM_MUX` (46), also known as `mux`

#### ( x1 x2 mux -- x3 )

This word's execution results in a mux value being popped off the top of the data stack followed by two other values, where the lower of these values is anded against the mux value and or'ed to the higher of these values anded to the inverse of the mux value is then pushed onto the top of the data stack.

### `HF_PRIM_RMUX` (47), also known as `/mux`

#### ( mux x1 x2 -- x3 )

This word's execution results in two values being popped off the top of the data stack, followed by a mux value, where the lower of these values is anded against the mux value and orded to the higher of these values anded to the inverse of the mux value is then pushed onto the top of the data stack.

### `HF_PRIM_LOAD_R` (48), also known as `r@`

#### ( R: x -- x ) ( -- x )

This word's execution results in the top-most value on the return stack being pushed onto the data stack without being popped from the return stack.

### `HF_PRIM_PUSH_R` (49), also known as `>r`

#### ( x -- ) ( R: -- x )

This word's execution results in a value being popped off the top of the data stack and being pushed onto the return stack.

### `HF_PRIM_POP_R` (50), also known as `r>`

#### ( R: x -- ) ( -- x )

This word's execution results in a value being popped off the top of the return stack and being pushed onto the data stack.

### `HF_PRIM_LOAD_SP` (51), also known as `sp@`

#### ( -- addr )

This word's execution results in the data stack pointer prior to the execution of this word being pushed onto the data stack.

### `HF_PRIM_STORE_SP` (52), also known as `sp!`

#### ( addr -- )

This word's execution results in the data stack pointer being set to an address popped off the top of the data stack.

### `HF_PRIM_LOAD_RP` (53), also known as `rp@`

#### ( -- addr )

This word's execution results in the return stack pointer being pushed onto the data stack.

### `HF_PRIM_STORE_RP` (54), also known as `rp!`

#### ( addr -- )

This word's execution results in the return stack pointer being set to an address popped off the top of the data stack.

### `HF_PRIM_TO_BODY` (55), also known as `>body`

#### ( xt -- addr )

This word's execution results in a token being popped off the top of the data stack and the data pointer for the word referred to by the token being subsequently pushed onto the data stack.

### `HF_PRIM_LOAD_16` (56), also known as `h@`

#### ( addr -- h )

This word's execution results in an address being popped off the top of the data stack, which is then dereferenced and the 16-bit value at that location is then pushed onto the data stack. Note that no sign extension takes place.

### `HF_PRIM_STORE_16` (57), also known as `h!`

#### ( h addr -- )

This word's execution results in an address being popped off the top of the data stack, followed by a value being popped off the data stack from beneath it; afterwards, the address is dereferenced and the 16-bit value at that location is set to the lower 16 bits of the value that was popped.

### `HF_PRIM_LOAD_32` (58), also known as `w@`

#### ( addr -- w )

This word's execution results in an address being popped off the top of the data stack, which is then dereferenced and the 32-bit value at that location is then pushed onto the data stack. Note that no sign extension takes place.

### `HF_PRIM_STORE_32` (59), also known as `w!`

#### ( w addr -- )

This word's execution results in an address being popped off the top of the data stack, followed by a value being popped off the data stack from beneath it; afterwards, the address is dereferenced and the 32-bit value at that location is set to the lower 32 bits of the value that was popped.

### `HF_PRIM_SET_WORD_COUNT` (60), also known as `set-word-count`

#### ( u -- )

This word's execution results in the number of words in the word table being set to a count popped off the top of the data stack; this count must be less than or equal to the number of words in the word table.

### `HF_PRIM_SYS` (61), also known as `sys`

#### ( ? n -- ? f )

This word's execution results in a service number being popped off the top of the data stack, which is non-negative for standard services and negative for non-standard services, which if corresponding to an implemented service results in the parameters for that service being popped off the top of the data stack and the results being pushed onto the data stack followed by -1 being pushed onto the data stack, and otherwise 0 is pushed onto the data stack.

### `HF_PRIM_D_ADD` (62), also known as `d+`

####  ( l1 h1 l2 h2 -- l3 h3 )

This word's execution results in the addition of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting double-cell integer being pushed back onto the data stack.

### `HF_PRIM_D_SUB` (63), also known as `d-`

####  ( l1 h1 l2 h2 -- l3 h3 )

This word's execution results in the subtraction of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting double-cell integer being pushed back onto the data stack.

### `HF_PRIM_D_MUL` (64), also known as `d*`

####  ( l1 h1 l2 h2 -- l3 h3 )

This word's execution results in the multiplication of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting double-cell integer being pushed back onto the data stack.

### `HF_PRIM_D_DIV` (65), also known as `d/`

####  ( l1 h1 l2 h2 -- l3 h3 )

This word's execution results in the signed division of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting double-cell integer being pushed back onto the data stack.

### `HF_PRIM_D_MOD` (66), also known as `dmod`

####  ( l1 h1 l2 h2 -- l3 h3 )

This word's execution results in the signed modulus of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting double-cell integer being pushed back onto the data stack.

### `HF_PRIM_D_UDIV` (67), also known as `du/`

####  ( l1 h1 l2 h2 -- l3 h3 )

This word's execution results in the unsigned division of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting double-cell integer being pushed back onto the data stack.

### `HF_PRIM_D_UMOD` (68), also known as `dumod`

####  ( l1 h1 l2 h2 -- l3 h3 )

This word's execution results in the unsigned modulus of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting double-cell integer being pushed back onto the data stack.

### `HF_PRIM_D_NOT` (69), also known as `dnot`

#### ( l1 h1 -- l2 h2 )

This word's execution results in the binary inverse of a double-cell integer popped off the data stack being pushed back onto the data stack.

### `HF_PRIM_D_AND` (70), also known as `dand`

####  ( l1 h1 l2 h2 -- l3 h3 )

This word's execution results in the binary and of two double-cell integers popped off the data stack and with the resulting double-cell integer being pushed back onto the data stack.

### `HF_PRIM_D_OR` (71), also known as `dor`

####  ( l1 h1 l2 h2 -- l3 h3 )

This word's execution results in the binary or of two double-cell integers popped off the data stack and with the resulting double-cell integer being pushed back onto the data stack.

### `HF_PRIM_D_XOR` (72), also known as `dor`

####  ( l1 h1 l2 h2 -- l3 h3 )

This word's execution results in the binary exclusive or of two double-cell integers popped off the data stack and with the resulting double-cell integer being pushed back onto the data stack.

### `HF_PRIM_D_LSHIFT` (73), also known as `dlshift`

#### ( l1 h1 u -- l2 h2 )

This word's execution results in the left shift by an integer first popped off the data stack of a double cell integer, with the highest significance cell higher on the data stack, popped afterwards off the data stack, with the resulting double cell integer pushed back onto the data stack.

### `HF_PRIM_D_RSHIFT` (74), also known as `drshift`

#### ( l1 h1 u -- l2 h2 )

This word's execution results in the logical right shift by an integer first popped off the data stack of a double cell integer, with the highest significance cell higher on the data stack, popped afterwards off the data stack, with the resulting double cell integer pushed back onto the data stack.

### `HF_PRIM_D_ARSHIFT` (75), also known as `darshift`

#### ( l1 h1 u -- l2 h2 )

This word's execution results in the arithmetic right shift by an integer first popped off the data stack of a double cell integer, with the highest significance cell higher on the data stack, popped afterwards off the data stack, with the resulting double cell integer pushed back onto the data stack.

### `HF_PRIM_D_LT` (76), also known as `d<`

####  ( l1 h1 l2 h2 -- f )

This word's execution results in the signed less-than comparison of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting boolean being pushed onto the data stack.

### `HF_PRIM_D_GT` (77), also known as `d>`

####  ( l1 h1 l2 h2 -- f )

This word's execution results in the signed greater-than comparison of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting boolean being pushed onto the data stack.

### `HF_PRIM_D_EQ` (78), also known as `d=`

####  ( l1 h1 l2 h2 -- f )

This word's execution results in the equals comparison of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting boolean being pushed onto the data stack.

### `HF_PRIM_D_NE` (79), also known as `d<>`

####  ( l1 h1 l2 h2 -- f )

This word's execution results in the not-equals comparison of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting boolean being pushed onto the data stack.

### `HF_PRIM_D_ULT` (80), also known as `du<`

####  ( l1 h1 l2 h2 -- f )

This word's execution results in the unsigned less-than comparison of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting boolean being pushed onto the data stack.

### `HF_PRIM_D_UGT` (81), also known as `du>`

####  ( l1 h1 l2 h2 -- f )

This word's execution results in the unsigned greater-than comparison of two double-cell integers, with the highest significance cell of each higher on the stack, popped off the data stack and with the resulting boolean being pushed onto the data stack.

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

Exit the Forth runtime.

### `HF_SYS_OPEN` (3)

#### ( addr u flags mode -- fd f )

Open a file with the specified flags and mode, as specified by a mode popped off the top of the data stack, followed by flags, and then name length and a pointer to the start of the name. The mode is an OS-specific value which may indicate permissions for newly created files. The flags are one of `HF_OPEN_RDONLY` (1), indicating to open the file in read-only mode, `HF_OPEN_WRONLY` (2), indicating to open the file in write-only mode, or `HF_OPEN_RDWR` (4), indicating to open the file in read-write mode, which if `HF_OPEN_WRONLY` is set, may be or'ed with `HF_OPEN_APPEND` (8) to put the file in appending mode, and if either `HF_OPEN_WRONLY` or `HF_OPEN_RDWR` is set, may be or'ed with `HF_OPEN_CREAT` (16) to specify that a file may be created if it does not already exist, with `HF_OPEN_EXCL` (32) combined with `HF_OPEN_CREAT` to specify that a file *must* be created or else opening will fail, and/or with `HF_OPEN_TRUNC` (64) to specify that if the file already exists and is a regular file the file will be truncated to length 0. If opening the file is successful the file descriptor followed by -1 are pushed onto the data stack, otherwise 0 followed by 0 are pushed onto the data stack.

### `HF_SYS_CLOSE` (4)

#### ( fd -- f )

Close a file, as specified by a file descriptor popped off the top of the data stack, and push -1 onto the data stack if successful, or otherwise push 0 onto the data stack.

### `HF_SYS_READ` (5)

#### ( addr u fd -- u f )

Read from a file descriptor, as specified by a file descriptor popped off the top of the data stack, followed by the maximum number of bytes to read and then a pointer to the start of a buffer into which bytes are to be read. The total number of bytes read, 0 in the case of a closed file descriptor, an error, or a non-blocking file descriptor that would block, is pushed onto the data stack, followed by -1 for a successful read or a closed file descriptor, 1 for a non-blocking file descriptor that would block, or 0 for an error.

### `HF_SYS_WRITE` (6)

#### ( addr u fd -- u f )

Write to a file descriptor, as specified by a file descriptor popped off the top of the data stack, followed by the maximum number of bytes to write and then a pointer to the start of a buffer from which bytes are to be written. The total number of bytes written is pushed onto the data stack, followed by -1 for a successful write, 1 for a non-blocking file descriptor that would block, or 0 for an error.

### `HF_SYS_GET_NONBLOCKING` (7)

#### ( fd -- f1 f2 )

Get whether a file descriptor is non-blocking, as specified by a file descriptor popped off the top of the data stack. -1 is pushed onto the data stack if the file descriptor is non-blocking, otherwise 0 is pushed onto the data stack; afterwards, -1 is pushed onto the data stack if the operation was successful, otherwise 0 is pushed onto the data stack.

### `HF_SYS_SET_NONBLOCKING` (8)

#### ( f1 fd -- f2 )

Set whether a file descriptor is non-blocking, as specified by a file descriptor popped off the top of the data stack followed by a flag indicating whether the file descriptor is to be non-blocking (non-zero values result in the file descriptor being set to be non-blocking and zero results in the file descriptor being set to be blocking). -1 is pushed onto the data stack if the operation was successful, otherwise 0 is pushed onto the data stack.

### `HF_SYS_ISATTY` (9)

#### ( fd -- f1 f2 )

Get whether a file descriptor corresponds to a terminal, as specified by a file descriptor popped off the top of the data stack. -1 is pushed onto the data stack if the file descriptor corresponds to a terminal, otherwise 0 is pushed onto the data stack; afterwards, -1 is pushed onto the data stack if the operation was successful, otherwise 0 is pushed onto the data stack.

### `HF_SYS_POLL` (10)

#### ( addr u1 ms -- u2 f )

Wait for IO or a timeout, as specified by a number of milliseconds to wait (-1 to wait forever) popped off the top of the data stack, followed by a number of file descriptor entries to wait on and then a pointer to a data structure containing the information indicating what file descriptors to wait on, what events to wait for, and what events occurred. The data structure consists of `u1` entries, each consisting of three cells, the first cell being a file descriptor, the second cell being any of `HF_POLL_IN` (1), indicating to wait for input, `HF_POLL_OUT` (2), indicating to wait for output, and/or `HF_POLL_PRI` (4), indicating to wait for priority input, or'ed together, and the third cell receiving any of `HF_POLL_IN`, indicating that input occurred, `HF_POLL_OUT`, indicating that output occurred, `HF_POLL_PRI`, indicating that priority input occurred, `HF_POLL_ERR` (8), indicating that an error occurred, `HF_POLL_HUP` (16), indicating that a hang-up occurred, or `HF_POLL_NVAL` (32), indicating that invalid input was provided, or'ed together. Afterwards, the number of file descriptors for which events occurred is pushed onto the data stack, followed by -1 for success and 0 for failure being pushed onto the data stack.

### `HF_SYS_GET_MONOTONIC_TIME` (11)

#### ( -- s ns )

Get a monotonic time, i.e. a time that is guaranteed to not have jumps, pushing the current number of seconds onto the data stack followed by pushing the current number of nanoseconds onto the data stack.

### `HF_SYS_GET_TRACE` (12)

#### ( -- f )

Get whether a debugging tracer is active, pushing -1 onto the data stack if it is active, and otherwise pushing 0 onto the data stack.

### `HF_SYS_SET_TRACE` (13)

#### ( f -- )

Set whether a debugging tracer is active, popping a flag off the top of the data stack, turning on debugging tracing if it is non-zero and the runtime is compiled to support debugging tracing, and otherwise turning off debugging tracing.

### `HF_SYS_GET_SBASE` (14)

#### ( -- addr )

Get the data stack base used by a debugging tracer, pushing it onto the data stack.

### `HF_SYS_SET_SBASE` (15)

#### ( addr -- )

Set the data stack base used by a debugging tracer, popping it off the top of the data stack.

### `HF_SYS_GET_RBASE` (16)

#### ( -- addr )

Get the return stack base used by a debugging tracer, pushing it onto the data stack.

### `HF_SYS_SET_RBASE` (17)

#### ( addr -- )

Set the return stack base used by a debugging tracer, popping it off the top of the data stack.

### `HF_SYS_GET_NAME_TABLE` (18)

#### ( -- addr )

Get the name table used by a debugging tracer, pushing it onto the data stack. The name table is an array of entries consisting of two cells, the first being a pointer to the first byte of a name and the second being the length of the name in bytes, where the array index is the token which the name corresponds to

### `HF_SYS_SET_NAME_TABLE` (19)

#### ( addr -- )

Set the name table used by a debugging tracer, popping it off the top of the data stack. The name table is an array of entries consisting of two cells, the first being a pointer to the first byte of a name and the second being the length of the name in bytes, where the array index is the token which the name corresponds to

### `HF_SYS_PREPARE_TERMINAL` (20)

#### ( fd -- f )

Prepare the specified file descriptor for non-canonical, non-echoing user input. Return -1 if successful, else return 0.

### `HF_SYS_CLEANUP_TERMINAL` (21)

#### ( fd -- f )

Restore the specific file descriptor to a canonical, echoing state. Return -1 if successful, else return 0.

### `HF_SYS_GET_TERMINAL_SIZE` (22)

#### ( fd -- rows cols xpixels ypixels f )

Get the size of a terminal specified by a file descriptor in rows, columns, x pixels, and y pixels. Also return -1 if successful, else return 0.

### `HF_SYS_GET_INT_HANDLER` (23)

#### ( int -- xt )

Get the handler of an interrupt with the specified index. Invalid interrupt indices will result in 0 being returned.

### `HF_SYS_SET_INT_HANDLER` (24)

#### ( xt int -- )

Set the handler of an interrupt with the specified index. Invalid interrupt indices will result in no action being taken.

### `HF_SYS_GET_INT_MASK` (25)

#### ( -- mask )

Get the current interrupt mask, where each bit corresponds to whether the interrupt with the bit's index is enabled (with 1 being enabled).

### `HF_SYS_SET_INT_MASK` (26)

#### ( mask -- )

Set the current interrupt mask, where each bit corresponds to whether the interrupt with the bit's index is enabled (with 1 being enabled).

### `HF_SYS_ADJUST_INT_MASK` (27)

#### ( mask-and mask-or -- )

Atomically both and and or the current interrupt mask, yielding a new current interrupt mask. First the interrupt mask is anded with the value second-highest on the stack, and then the interrupt mask is ored with the value highest on the stack.

### `HF_SYS_GET_INT_HANDLER_MASK` (28)

#### ( index -- mask )

Get the interrupt mask associated with an interrupt, whose index is popped off the stack. If an invalid interrupt index is specified 0 is returned.

### `HF_SYS_SET_INT_HANDLER_MASK` (29)

#### ( mask index -- )

Set the interrupt mask associated with an interrupt, whose index is popped off the stack followed by the interrupt mask to set. If an invalid interrupt index is specified this is ignored.

### `HF_SYS_GET_PROTECT_STACKS` (30)

#### ( -- f )

Set whether to protect the stacks when executing interrupt handlers, i.e. to allow interrupt handlers to execute even when there is severe data or return stack underflow.

### `HF_SYS_SET_PROTECT_STACKS` (31)

#### ( f -- )

Get whether to protect the stacks when executing interrupt handlers, i.e. to allow interrupt handlers to execute even when there is severe data or return stack underflow.

### `HF_SYS_GET_ALARM` (32)

#### ( alarm -- interval-s interval-ns value-s value-ns success )

Get the settings of an alarm, including alarm interval in seconds and nanoseconds and alarm value (time until next alarm trigger) in seconds and nanoseconds, along with `TRUE` on success. In the case of an invalid alarm type, filler values and `FALSE` are returned.

### `HF_SYS_SET_ALARM` (33)

#### ( new-interval-s new-interval-ns new-value-s new-value-ns alarm -- old-interval-s old-interval-ns old-value-s old-value-ns success )

Set new settings of an alarm, including alarm interval in seconds and nanoseconds and alarm value (time until next alarm trigger) in seconds and nanoseconds, and return old settings of the same, along with `TRUE` on success. In the case of an invalid alarm type, filler values and `FALSE` are returned and no action is taken.

## Interrupts

### `HF_INT_SEGV` (0)

Segmentation fault.

### `HF_INT_TOKEN` (1)

Attempted to execute an invalid token.

### `HF_INT_DIVZERO` (2)

Attempted to divide by zero.

### `HF_INT_ILLEGAL` (3)

Illegal instruction.

### `HF_INT_BUS` (4)

Bus error.

### `HF_INT_ALARM_REAL` (5)

Real-time alarm.

### `HF_INT_ALARM_VIRTUAL` (6)

User time-only alarm.

### `HF_INT_ALARM_PROF` (7)

User and system-time alarm.

## Alarm types

### `HF_ALARM_REAL` (0)

Real-time alarm.

### `HF_ALARM_VIRTUAL` (1)

User time-only alarm.

### `HF_ALARM_PROF` (2)

User and system-time alarm.