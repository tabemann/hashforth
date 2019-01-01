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
#include <stdio.h>
#include <string.h>
#include <stdint.h>
#include "hf/common.h"

/* Initialize hashforth */
void hf_init(hf_global_t* global) {
  hf_cell_t* data_stack_base;
  hf_token_t** return_stack_base;
  global->word_count = 0;
  global->word_space_count = HF_INIT_WORD_SPACE_COUNT;
  if(!(global->words = malloc(sizeof(hf_word_t) * global->word_space_count))) {
    fprintf(stderr, "Unable to allocate words array!\n");
    abort();
  }
  global->wordlist_count = 1;
  global->wordlist_space_count = HF_INIT_WORDLIST_SPACE_COUNT;
  if(!(global->wordlists =
       malloc(sizeof(hf_wordlist_t) * global->wordlist_space_count))) {
    fprintf(stderr, "Unable to allocate wordlists array!\n");
    abort();
  }
  global->wordlists[0].first = NULL;
  global->current_wordlist = 0;
  global->current_word = NULL;
  global->ip = NULL;
  global->latestxt = NULL;
  if(!(data_stack_base =
       malloc(sizeof(hf_cell_t) * HF_INIT_DATA_STACK_COUNT))) {
    fprintf(stderr, "Unable to allocate data stack!\n");
    abort();
  }
  global->data_stack = data_stack_base + HF_INIT_DATA_STACK_COUNT;
  if(!(return_stack_base =
       malloc(sizeof(hf_token_t*) * HF_INIT_RETURN_STACK_COUNT))) {
    fprintf(stderr, "Unable to allocate return stack!\n");
    abort();
  }
  global->return_stack = return_stack_base + HF_INIT_RETURN_STACK_COUNT;
  if(!(global->user_space_start = global->user_space_current =
       malloc(HF_INIT_USER_SPACE_SIZE))) {
    fprintf(stderr, "Unable to allocate user space!\n");
    abort();
  }
  global->user_space_end = global->user_space_start + HF_INIT_USER_SPACE_SIZE;
}

/* The inner interpreter */
void hf_inner(hf_global_t* global) {
  while(global->ip) {
    hf_full_token_t token = *global->ip++;
#ifdef TOKEN_8_16
    if(token & 0x80) {
      token = (token & 0x7F) | ((hf_full_token_t)(*global->ip++) << 7);
    }
#else
#ifdef TOKEN_16_32
    if(token & 0x8000) {
      token = (token & 0x7FFF) | ((hf_full_token_t)(*global->ip++) << 15);
    }
#endif
#endif
    if(token < global->word_count) {
      af_word_t* word = global->words + token;
      global->current_word = word;
      word->primitive(global);
    } else {
      fprintf(stderr, "Invalid token!: %d\n", (int)token);
    }
  }
}

/* Allocate more user space if needed */
void hf_new_user_space(hf_global_t* global) {
  if(global->user_space_end - global->user_space_current <
     HF_WORD_MIN_USER_SPACE_LEFT) {
    if(!(global->user_space_start = global->user_space_current =
	 malloc(HF_INIT_USER_SPACE_SIZE))) {
      fprintf(stderr, "Unable to allocate user space!\n");
      abort();
    }
    global->user_space_end = global->user_space_start + HF_INIT_USER_SPACE_SIZE;
  }
}

/* Allocate data in user space */
void* hf_allocate(hf_global_t* global, hf_cell_t size) {
  void* current = global->user_space_current;
  global->user_space_current += size;
  return current;
}

/* Allocate a word */
hf_word_t* hf_new_word(hf_global_t* global, hf_full_token_t token) {
  if(token == global->word_space_count) {
    global->word_space_count *= 2;
    if(!(global->words = realloc(global->words,
				 sizeof(hf_word_t) *
				 global->word_space_count))) {
      fprintf(stderr, "Unable to allocate word space!\n");
      abort();
    }
  }
  global->word_count =
    token >= global->word_count ? token + 1 : global->word_count;
  return global->words + token;
}

/* Allocate a wordlist */
hf_wordlist_id_t hf_new_wordlist(hf_global_t* global) {
  if(global->wordlist_count == global->wordlist_space_count) {
    global->wordlist_space_count *= 2;
    if(!(global->wordlists = realloc(global->wordlists,
				     sizeof(hf_wordlist_t) *
				     global->wordlists_space_count))) {
      fprintf(stderr, "Unable to allocate wordlist space!\n");
      abort();
    }
  }
  global->wordlists[global->wordlist_count].first = 0;
  return global->wordlist_count++;
}

/* Allocate a token */
hf_full_token_t hf_new_token(hf_global_t* global) {
  hf_cell_t error = HF_FALSE;
#ifdef TOKEN_8_16
  if(global->word_count == 32768) {
    error = HF_TRUE;
  }
#else
#ifdef TOKEN_16
#ifdef CELL_16
  if(global->word_count == 65535) {
    error = HF_TRUE;
  }
#else
  if(global->word_count == 65536) {
    error = HF_TRUE;
  }
#endif
#else
#ifdef TOKEN_16_32
  if(global->word_count == 0x80000000) {
    error = HF_TRUE;
  }
#else
#ifdef CELL_32 /* This should never be reached under normal circumstances */
  if(global->word_count == 0xFFFFFFFF) {
    error = HF_TRUE;
  }
#endif
#endif
#endif
#endif
  if(error) {
    fprintf(stderr, "Out of available tokens!\n");
    abort();
  }
  return global->word_count++;
}
