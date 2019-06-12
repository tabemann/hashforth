\ Copyright (c) 2019, Travis Bemann
\ All rights reserved.
\ 
\ Redistribution and use in source and binary forms, with or without
\ modification, are permitted provided that the following conditions are met:
\ 
\ 1. Redistributions of source code must retain the above copyright notice,
\    this list of conditions and the following disclaimer.
\ 
\ 2. Redistributions in binary form must reproduce the above copyright notice,
\    this list of conditions and the following disclaimer in the documentation
\    and/or other materials provided with the distribution.
\ 
\ 3. Neither the name of the copyright holder nor the names of its
\    contributors may be used to endorse or promote products derived from
\    this software without specific prior written permission.
\ 
\ THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
\ AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
\ IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
\ ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
\ LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
\ CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
\ SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
\ INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
\ CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
\ ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
\ POSSIBILITY OF SUCH DAMAGE.

forth-wordlist vector-wordlist 2 set-order

4 256 allot-vector constant my-vector

true constant success
false constant failure

: dump-vector ( vector -- )
  >r ." < " 0 begin dup r@ count-vector < while
    [char] " emit here 256 allot over r@ get-vector drop -256 allot
    here count type [char] " emit space 1 +
  repeat
  drop ." > " r> drop ;

: prepare-block ( c-addr1 u c-addr2 -- )
  swap 255 min swap 2dup c! 1 + swap move ;

: push-start-entry ( success c-addr u vector -- )
  rot rot 2dup ." pushing-start " [char] " emit type [char] " emit
  here prepare-block here 256 allot over push-start-vector -256 allot
  space not rot xor if ." success " else ." failure " then dump-vector cr ;

: push-end-entry ( success c-addr u vector -- )
  rot rot 2dup ." pushing-end " [char] " emit type [char] " emit
  here prepare-block here 256 allot over push-end-vector -256 allot
  space not rot xor if ." success " else ." failure " then dump-vector cr ;

: pop-start-entry ( success vector -- )
  ." popping-start " here 256 allot over pop-start-vector -256 allot
  dup if [char] " emit here count type [char] " emit space then
  not rot xor if ." success " else ." failure " then dump-vector cr ;
  
: pop-end-entry ( success vector -- )
  ." popping-end " here 256 allot over pop-end-vector -256 allot
  dup if [char] " emit here count type [char] " emit space then
  not rot xor if ." success " else ." failure " then dump-vector cr ;

: peek-start-entry ( success vector -- )
  ." peeking-start " here 256 allot over peek-start-vector -256 allot
  dup if [char] " emit here count type [char] " emit space then
  not rot xor if ." success " else ." failure " then dump-vector cr ;
  
: peek-end-entry ( success vector -- )
  ." peeking-end " here 256 allot over peek-end-vector -256 allot
  dup if [char] " emit here count type [char] " emit space then
  not rot xor if ." success " else ." failure " then dump-vector cr ;

: vector-test ( -- )
  success s" foo" my-vector push-start-entry
  success s" bar" my-vector push-end-entry
  success s" baz" my-vector push-start-entry
  success s" qux" my-vector push-end-entry
  failure s" foobar" my-vector push-start-entry
  failure s" foobaz" my-vector push-end-entry
  success my-vector peek-start-entry
  success my-vector peek-end-entry
  success my-vector pop-start-entry
  success my-vector pop-end-entry
  success my-vector pop-start-entry
  success my-vector pop-end-entry
  failure my-vector peek-start-entry
  failure my-vector peek-end-entry
  failure my-vector pop-start-entry
  failure my-vector pop-end-entry ;

vector-test