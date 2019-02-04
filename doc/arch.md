# Hashforth Architecture

Hashforth is a token-threaded Forth with four core variants, 64-bit, 32-bit-large, 32-bit-small, and 16-bit. It has two different kinds of token, 8/16-bit and 16/32-bit, with 64-bit and 32-bit-large having 16/32-bit tokens and 32-bit-small and 16-bit having 8/16-bit tokens. As should be apparent, 64-bit has 64-bit cells, 32-bit-large and 32-bit-small have 32-bit cells, and 16-bit has 16-bit cells. None of these variants support any special size for characters, and what are treated as characters in other Forths are simply treated as 8-bit bytes. It does not impose any alignment restrictions, and implementations will need to handle alignment on their own if the architectures they are implemented on have alignment restrictions. It also is little-endian, and implementations will need to convert endianness if the architectures they are implemented on are big-endian.

Tokens have two sizes for both 16/32-bit tokens and 8/16-bit tokens. In both of these cases by default tokens are the smaller size, and in that size can represent 15 and 7 bits respectively. To represent more tokens, specifically 2147516416
tokens and 32896 tokens, the first 16 or 8 bits has its high bit set to 1 (whereas otherwise it would be set to 0), which serves as a flag to indicate the existence of more bits to follow, and it is followed by another 16 or 8 bits which together represent the higher bits of the token plus 1.

## Primitives

There exist the following primitives in hashforth; for all these primitives, if not specified otherwise, the interpreter pointer after the word's execution points to the token immediately after the token executed:

### 0: `HF_PRIM_END`, also known as `END`

#### ( -- )

This word should not be executed, and its execution indicates a bug in either the code being executed or the VM itself. Its execution results in an immediate exit.

### 1: `HF_PRIM_NOP`, also known as `NOP`

#### ( -- )

This word is a no-op.

### 2: `HF_PRIM_EXIT`, also known as `EXIT`

#### ( -- ) ( R: addr -- )

This word's execution results in the top of the return stack being popped and transferred to the interpreter pointer.

### 3: `HF_PRIM_BRANCH`, also known as `BRANCH`

#### ( -- )

This word's execution results in the cell immediately after the token for this word being transferred to the interpreter pointer.

### 4: `HF_PRIM_0BRANCH`, also known as `0BRANCH`

#### ( u -- )

This word's execution results in the value on the top of the data stack being popped; if its value is zero the cell immediately after the token for this word is transferred to the interpreter pointer; otherwise the interpreter pointer afterwards points to the token after the cell immediately after the token for this word.

### 5: `HF_PRIM_LIT`, also known as `(LIT)`

#### ( -- n )

This word's execution results in the value in the cell immediately after the token for this word being pushed onto the top of the data stack, and the interpreter pointer points to the token after the cell immediately after the token for this word.

### 6: `HF_PRIM_DATA`, also known as `(DATA)`

#### ( -- addr )

This word's execution results in the value in bytes in the cell immediately after the token for this word plus the size of one cell and the size of the token for this word being added to the interpreter pointer and the address of the byte immediately after the cell immediately after the token for this word being pushed onto the data stack.

### 7: `HF_PRIM_NEW_COLON`, also known as `NEW-COLON`

#### ( addr -- xt )

This word's execution results in a new anonymous colon word being created, with the pointer to the start of its token-threaded code being popped off the top of the data stack, the data pointer being set to 0, and the token for the newly created word being pushed onto the data stack. This newly created word also has no next word.

### 8: `HF_PRIM_NEW_CREATE`, also known as `NEW-CREATE`

#### ( addr -- xt )

This word's execution results in a new anonymous created word being created, with the pointer to the start of its data being popped off the top of the data stack, the TTC code pointer of that word being set to 0, and the token for the newly created word being pushed onto the data stack. This newly created word also has no next word. Created words when executed, by default, push the pointer to the start of their data onto the data stack.

### 9: `HF_PRIM_SET_DOES`, also known as `SET-DOES>`

#### ( xt -- ) ( R: exit-addr does-addr -- )

This word's execution results in a token for a word being popped off the top of the data stack, an address being popped off the top of the return stack and being set to the TTC code pointer of that word, an address afterwards being popped off the top of the return stack and the interpreter pointer being set to it, and the word being set to being a `CREATE`/`DOES>` word which when executed pushes the pointer to the start of their data onto the data stack and then executes the token-threaded code at the TTC code pointer of the word.

### 10: `HF_PRIM_FINISH`, also known as `FINISH`

#### ( xt -- )

This word's execution results in a token for a word being popped off the top of the data stack, and any necessary finalization of the word (other than the compilation of `EXIT` and `END`, which must be done before this is called) taking place, such as JIT compilation. In the current implementation, this is equivalent to `HF_PRIM_DROP`.

### 11: `HF_PRIM_EXECUTE`, also known as `EXECUTE`

#### ( ? xt -- ? )

This word's execution results in a token for a word being popped off the top of the data stack, and that word being executed. If the word is not a primitive, the interpreter pointer as it would be after the execution of a normal word is pushed onto the return stack.

### 12: `HF_PRIM_DROP`, also known as `DROP`

#### ( n -- )

This word's execution results in a cell being popped off the data stack and then being discarded.

### 13: `HF_PRIM_DUP`, also known as `DUP`

#### ( n1 -- n1 n1 )

This word's execution results in the cell on the top of the data stack being duplicated as a new cell on the top of the data stack.

### 14: `HF_PRIM_SWAP`, also known as `SWAP`

#### ( n1 n2 -- n2 n1 )

This word's execution results in the two cells on the top of the data stack exchanging places.

### 15: `HF_PRIM_ROT`, also known as `ROT`

#### ( n1 n2 n3 -- n2 n3 n1 )

This word's execution results in third-from-the-top cell in the data stack being removed and being copied onto the top of the data stack, resulting in the second-from-the-top cell in the data stack now being in the third-from-the-top position and the cell on the top of the data stack now being in the second-from-the-top position.

### 16: `HF_PRIM_PICK`, also known as `PICK`

#### ( ni ... n0 i -- ni ... n0 ni )

This word's execution results in an index being popped off the top of the data stack, afterwards which serves as a location relative to the top of the data stack, with zero being the top of the data stack after the index was removed, where the cell at that location is copied and placed on the top of the data stack.

### 17: `HF_PRIM_ROLL`, also known as `ROLL`

#### ( ni ni-1 ... n0 i -- ni-1 ... n0 ni )

This word's execution results in an index being popped off the top of the data stack, afterwards which serves as a location relative to the top of the data stack, with zero being the top of the data stack after the index was removed, where the cell at that location is removed and placed on the top of the data stack.

### 18: `HF_PRIM_LOAD`, also known as `@`

#### ( addr -- n )

This word's execution results in an address being popped off the top of the data stack, which is then dereferenced and the cell at that location is then pushed onto the data stack.

### 19: `HF_PRIM_STORE`, also known as `!`

#### ( n addr -- )

This word's execution results in an address being popped off the top of the data stack, followed by a value being popped off the data stack from beneath it; afterwards, the address is dereferenced and the cell at that location is set to the value that was popped.

### 20: `HF_PRIM_LOAD_8`, also known as `C@`

#### ( addr -- c )

This word's execution results in an address being popped off the top of the data stack, which is then dereferenced and the byte at that location is then pushed onto the data stack. Note that no sign extension takes place.

### 21: `HF_PRIM_STORE_8`, also known as `C!`

#### ( c addr -- )

This word's execution results in an address being popped off the top of the data stack, followed by a value being popped off the data stack from beneath it; afterwards, the address is dereferenced and the byte at that location is set to the lower 8 bits of the value that was popped.

### 22: `HF_PRIM_EQ`, also known as `=`

#### ( n1 n2 -- f )

This word's execution results in two values being popped off the top of the data stack, and if they are equal -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### 23: `HF_PRIM_NE`, also known as `<>`

#### ( n1 n2 -- f )

This word's execution results in two values being popped off the top of the data stack, and if they are not equal -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### 24: `HF_PRIM_LT`, also known as `<`

#### ( n1 n2 -- f )

This word's execution results in two signed values being popped off the top of the data stack, and if the lower value is (signed) smaller than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### 25: `HF_PRIM_GT`, also known as `>`

#### ( n1 n2 -- f )

This word's execution results in two signed values being popped off the top of the data stack, and if the lower value is (signed) greater than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### 26: `HF_PRIM_ULT`, also known as `U<`

#### ( u1 u2 -- f )

This word's execution results in two unsigned values being popped off the top of the data stack, and if the lower value is (unsigned) smaller than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.

### 27: `HF_PRIM_UGT`, also known as `U>`

#### ( u1 u2 -- f )

This word's execution results in two unsigned values being popped off the top of the data stack, and if the lower value is (unsigned) greater than the top value -1 is pushed onto the data stack; otherwise 0 is pushed onto the data stack.