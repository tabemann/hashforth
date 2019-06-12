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

get-order get-current base @

forth-wordlist buffer-wordlist 2 set-order
forth-wordlist set-current

\ Default buffer size
1024 constant to-lower-buffer-size

\ Default filename buffer size
255 constant filename-buffer-size

\ File loading failure exception
: x-file-loading-failure ( -- ) space ." failed to load file" cr ;

\ File saving failure exception
: x-file-saving-failure ( -- ) space ." failed to save file" cr ;

\ Advance one character
: advance-char ( addr1 byte1 -- addr2 byte2 ) 1 - swap 1 + swap ;

\ Skip whitespace
: skip-whitespace ( addr1 bytes1 -- addr2 bytes2 )
  begin
    dup 0 > if
      over c@ dup bl = over tab = or swap newline = or not
      dup not if rot rot advance-char rot then
    else
      true
    then
  until ;

\ Skip until a certain byte
: skip-to ( addr1 bytes1 c -- addr2 bytes2 )
  >r begin
    dup 0 > if
      over c@ r@ = rot rot advance-char rot
    else
      true
    then
  until
  r> drop ;

\ Get a word
: get-word ( addr1 bytes1 -- addr2 bytes2 word-addr word-bytes )
  2dup swap >r >r begin
    dup 0 > if
      over c@ dup bl = over tab = or swap newline = or
      dup not if rot rot advance-char rot then
    else
      true
    then
  until
  r> over - r> swap ;

\ Downcase a string
: downcase ( addr bytes -- )
  begin dup 0 > while
    over c@ dup [char] A >= over [char] Z <= and if
      [char] A - [char] a + 2 pick c!
    else
      drop
    then
    advance-char
  repeat
  2drop ;

\ s" constant
2 constant s"-length
create s"-data char s c, char " c,
s"-data s"-length 2constant s"-constant

\ ." constant
2 constant ."-length
create ."-data char c c, char " c,
."-data ."-length 2constant ."-constant

\ c" constant
2 constant c"-length
create c"-data char c c, char " c,
c"-data c"-length 2constant c"-constant

\ Convert data into being lowercase
: to-lower ( addr bytes -- )
  begin dup 0 > while
    skip-whitespace dup 0 > if
      get-word 2dup downcase case
	s"-constant ofstr [char] " skip-to endof
	."-constant ofstr [char] " skip-to endof
	c"-constant ofstr [char] " skip-to endof
	s" (" ofstr [char] ) skip-to endof
	s" .(" ofstr [char] ) skip-to endof
	s" \" ofstr newline skip-to endof
	s" char" ofstr skip-whitespace get-word 2drop endof
	s" [char]" ofstr skip-whitespace get-word 2drop endof
	s" include" ofstr skip-whitespace get-word 2drop endof
	s" require" ofstr skip-whitespace get-word 2drop endof
      endcasestr
    then
  repeat
  2drop ;

\ Save a file
: save-file ( data-addr data-bytes path-addr path-bytes -- )
  open-wronly open-creat or open-trunc or /666 open
  averts x-file-saving-failure
  dup >r wait-write-full nip averts x-file-saving-failure
  r> close averts x-file-saving-failure ;

\ Convert a file into having lowercase identifiers
: to-lower-file ( path-addr path-bytes -- )
  to-lower-buffer-size new-buffer
  2 pick 2 pick 2 pick read-file-into-buffer averts x-file-loading-failure
  dup get-buffer to-lower
  filename-buffer-size new-buffer
  3 pick 3 pick 2 pick append-buffer
  s" .tolower" 2 pick append-buffer
  over get-buffer 2 pick get-buffer save-file
  destroy-buffer destroy-buffer 2drop ;

base ! set-current set-order