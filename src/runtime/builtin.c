/* Copyright (c) 2018, Travis Bemann
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of the copyright holder nor the names of its
 *    contributors may be used to endorse or promote products derived from
 *    this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE. */

#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include "hf/common.h"
#include "hf/inner.h"
#include "hf/prim.h"
#include "hf/builtin.h"

/* Declarations */

/* Register a built-in */
hf_full_token_t hf_register_builtin(hf_global_t* global, char* name,
				    hf_flags_t flags,
                                    hf_wordlist_id_t wordlist);

/* Register a built-in variable or buffer */
hf_full_token_t hf_register_builtin_var(hf_global_t* global, char* name,
					size_t size, hf_flags_t flags,
					hf_wordlist_id_t wordlist);

/* Compile a token */
void hf_compile_token(hf_global_t* global, hf_full_token_t token);

/* Compile an argument */
void hf_compile_arg(hf_global_t* global, hf_cell_t value);

/* Compile the end of a word */
void hf_compile_end(hf_global_t* global);

/* Reserve a forward reference */
hf_cell_t* hf_reserve_forward(hf_global_t* global);

/* Get an address for a backwards reference */
hf_token_t* hf_prepare_backward(hf_global_t* global);

/* Resolve a forward reference */
void hf_resolve_forward(hf_global_t* global, hf_cell_t* forward);

/* Compile a backward reference */
void hf_compile_backward(hf_global_t* global, hf_token_t* backward);

/* Compile NEGATE */
void hf_compile_negate(hf_global_t* global);

/* Compile ALLOT */
void hf_compile_allot(hf_global_t* global);

/* Compile , */
void hf_compile_comma(hf_global_t* global);

/* Compile C, */
void hf_compile_comma_8(hf_global_t* global);

/* Compile H, */
void hf_compile_comma_16(hf_global_t* global);

/* Compile W, */
void hf_compile_comma_32(hf_global_t* global);

/* Compile COMPILE, */
void hf_compile_compile_comma(hf_global_t* global);

/* Compile IF */
void hf_compile_if(hf_global_t* global);

/* Compile ELSE */
void hf_compile_else(hf_global_t* global);

/* Compile THEN */
void hf_compile_then(hf_global_t* global);

/* Built-in non-primitive words */

hf_full_token_t hf_builtin_negate = 0;
hf_full_token_t hf_builtin_allot = 0;
hf_full_token_t hf_builtin_comma = 0;
hf_full_token_t hf_builtin_comma_8 = 0;
hf_full_token_t hf_builtin_comma_16 = 0;
hf_full_token_t hf_builtin_comma_32 = 0;
hf_full_token_t hf_builtin_compile_comma = 0;
hf_full_token_t hf_builtin_if = 0;
hf_full_token_t hf_builtin_else = 0;
hf_full_token_t hf_builtin_then = 0;

/* Definitions */

/* Register a built-in */
hf_full_token_t hf_register_builtin(hf_global_t* global, char* name,
				    hf_flags_t flags,
				    hf_wordlist_id_t wordlist) {
  hf_cell_t name_length = 0;
  void* name_space = NULL;
  hf_word_t* word;
  hf_full_token_t token = hf_new_token(global);
  hf_new_user_space(global);
  if(name) {
    name_length = strlen(name);
    name_space = hf_allocate(global, name_length);
    memcpy(name_space, name, name_length);
  }
  word = hf_new_word(global, token);
  word->flags = flags;
  word->name_length = name_length;
  word->name = name_space;
  word->data = NULL;
  word->primitive = hf_prim_enter;
  word->secondary = global->user_space_current;
  word->next = global->wordlists[wordlist].first;
  global->wordlists[wordlist].first = token;
  return token;
}

/* Register a built-in variable or buffer */
hf_full_token_t hf_register_builtin_var(hf_global_t* global, char* name,
					hf_flags_t flags,
					hf_wordlist_id_t wordlist) {
  hf_cell_t name_length = 0;
  void* name_space = NULL;
  hf_word_t* word;
  hf_full_token_t token = hf_new_token(global);
  if(name) {
    hf_new_user_space(global);
    name_length = strlen(name);
    name_space = hf_allocate(global, name_length);
    memcpy(name_space, name, name_length);
  }
  word = hf_new_word(global, token);
  word->flags = flags;
  word->name_length = name_length;
  word->name = name_space;
  word->data = global->user_space_current;
  word->primitive = hf_prim_do_create;
  word->secondary = NULL;
  word->next = global->wordlists[wordlist].first;
  global->wordlists[wordlist].first = token;
  global->user_space_current
  return token;
}

/* Compile a token */
void hf_compile_token(hf_global_t* global, hf_full_token_t token) {
#ifdef TOKEN_8_16
  if(token < 128) {
    *((af_token_t*)global->user_space_current)++ = token;
  } else {
    *((af_token_t*)global->user_space_current)++ = (token & 0x7F) | 0x80;
    *((af_token_t*)global->user_space_current)++ = (token >> 7) & 0xFF;
  }
#else
#ifdef TOKEN_16_32
  if(token < 32768) {
    *((af_token_t*)global->user_space_current)++ = token;
  } else {
    *((af_token_t*)global->user_space_current)++ = (token & 0x7FFF) | 0x8000;
    *((af_token_t*)global->user_space_current)++ = (token >> 15) & 0xFFFF;    
  }
#else
  *((af_token_t*)global->user_space_current)++ = token;
#endif
#endif
}

/* Compile an argument */
void hf_compile_arg(hf_global_t* global, hf_cell_t value) {
  *((af_cell_t*)global->user_space_current)++ = value;
}

/* Compile word exit and end */
void hf_compile_end(hf_global_t* global) {
  hf_compile_token(global, HF_PRIM_EXIT);
  hf_compile_token(global, HF_PRIM_END);
}

/* Reserve a forward reference */
hf_forward_t hf_reserve_forward(hf_global_t* global) {
  return ((hf_forward_t)global->user_space_current)++;
}

/* Get an address for a backwards reference */
hf_backward_t hf_prepare_backward(hf_global_t* global) {
  return (hf_backward_t)global->user_space_current;
}

/* Resolve a forward reference */
void hf_resolve_forward(hf_global_t* global, hf_forward_t forward) {
  *forward = (hf_cell_t)global->user_space_current;
}

/* Compile a backward reference */
void hf_compile_backward(hf_global_t* global, hf_backward_t backward) {
  hf_compile_arg((af_cell_t)backward);
}

/* Compile NEGATE */
void hf_compile_negate(hf_global_t* global) {
  hf_builtin_negate = hf_register_builtin(global, "NEGATE", HF_WORD_NORMAL,
					  HF_WORDLIST_FORTH);
  hf_compile_token(global, HF_PRIM_LITERAL);
  hf_compile_arg(global, 1);
  hf_compile_token(global, HF_PRIM_SUB);
  hf_compile_token(global, HF_PRIM_NOT);
  hf_compile_end(global);
}

/* Compile ALLOT */
void hf_compile_allot(hf_global_t* global) {
  hf_builtin_allot = hf_register_builtin(global, "ALLOT", HF_WORD_NORMAL,
					 HF_WORDLIST_FORTH);
  hf_compile_token(global, HF_PRIM_LOAD_HERE);
  hf_compile_token(global, HF_PRIM_ADD);
  hf_compile_token(global, HF_PRIM_STORE_HERE);
  hf_compile_end(global);
}

/* Compile , */
void hf_compile_comma(hf_global_t* global) {
  hf_builtin_comma = hf_register_builtin(global, ",", HF_WORD_NORMAL,
					 HF_WORDLIST_FORTH);
  hf_compile_token(global, HF_PRIM_LOAD_HERE);
  hf_compile_token(global, HF_PRIM_STORE);
  hf_compile_token(global, HF_PRIM_CELL_SIZE);
  hf_compile_token(global, hf_builtin_allot);
  hf_compile_end(global);
}

/* Compile C, */
void hf_compile_comma_8(hf_global_t* global) {
  hf_builtin_comma_8 = hf_register_builtin(global, "C,", HF_WORD_NORMAL,
					 HF_WORDLIST_FORTH);
  hf_compile_token(global, HF_PRIM_LOAD_HERE);
  hf_compile_token(global, HF_PRIM_STORE_8);
  hf_compile_token(global, HF_PRIM_LITERAL);
  hf_compile_arg(global, 1);
  hf_compile_token(global, hf_builtin_allot);
  hf_compile_end(global);
}

/* Compile H, */
void hf_compile_comma_16(hf_global_t* global) {
  hf_builtin_comma_16 = hf_register_builtin(global, "H,", HF_WORD_NORMAL,
					    HF_WORDLIST_FORTH);
  hf_compile_token(global, HF_PRIM_LOAD_HERE);
  hf_compile_token(global, HF_PRIM_STORE_16);
  hf_compile_token(global, HF_PRIM_LITERAL);
  hf_compile_arg(global, 2);
  hf_compile_token(global, hf_builtin_allot);
  hf_compile_end(global);
}

/* Compile W, */
void hf_compile_comma_32(hf_global_t* global) {
  hf_builtin_comma_32 = hf_register_builtin(global, "W,", HF_WORD_NORMAL,
					    HF_WORDLIST_FORTH);
  hf_compile_token(global, HF_PRIM_LOAD_HERE);
  hf_compile_token(global, HF_PRIM_STORE_32);
  hf_compile_token(global, HF_PRIM_LITERAL);
  hf_compile_arg(global, 4);
  hf_compile_token(global, hf_builtin_allot);
  hf_compile_end(global);
}

/* Compile COMPILE, */
void hf_compile_compile_comma(hf_global_t* global) {
#ifdef TOKEN_8_16
  hf_forward_t big_token_ref;
  hf_forward_t small_token_ref;

  hf_compile_token(global, HF_PRIM_DUP);
  hf_compile_token(global, HF_PRIM_LITERAL);
  hf_compile_arg(global, 0x80);
  hf_compile_token(global, HF_PRIM_ULT);
  hf_compile_token(global, HF_PRIM_0BRANCH);
  big_token_ref = hf_reserve_forward(global);
  hf_compile_token(global, hf_builtin_comma_8);
  hf_compile_token(global, HF_PRIM_BRANCH);
  small_token_ref = hf_reserve_forward(global);
  hf_resolve_forward(global, big_token_ref);
  hf_compile_token(global, HF_PRIM_DUP);
  hf_compile_token(global, HF_PRIM_LITERAL);
  hf_compile_arg(global, 0x7F);
  hf_compile_token(global, HF_PRIM_AND);
  hf_compile_token(global, HF_PRIM_LITERAL);
  hf_compile_arg(global, 0x80);
  hf_compile_token(global, HF_PRIM_OR);
  hf_compile_token(global, hf_builtin_comma_8);
  hf_compile_token(global, HF_PRIM_LITERAL);
  hf_compile_arg(global, 7);
  hf_compile_token(global, HF_PRIM_RSHIFT);
  hf_compile_token(global, HF_PRIM_LITERAL);
  hf_compile_arg(global, 0xFF);
  hf_compile_token(global, HF_PRIM_AND);
  hf_compile_token(global, hf_builtin_comma_8);
  hf_resolve_forward(global, small_token_ref);
  hf_compile_end(global);
  
#else
#ifdef TOKEN_16

#else
#ifdef TOKEN_16_32

#else /* TOKEN_32 */

#endif
#endif
#endif
}

/* Compile IF */
void hf_compile_if(hf_global_t* global);

/* Compile ELSE */
void hf_compile_else(hf_global_t* global);

/* Compile THEN */
void hf_compile_then(hf_global_t* global);

