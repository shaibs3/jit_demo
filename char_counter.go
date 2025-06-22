package main

import (
	"fmt"
	"os"
	"sort"
	"strings"
)

func countCharOccurrences(input string) map[rune]int {
	occurrences := make(map[rune]int)
	for _, char := range input {
		if char != ' ' { // Skip spaces
			occurrences[char]++
		}
	}
	return occurrences
}

func buildCharCountOutput(occurrences map[rune]int) string {
	if len(occurrences) == 0 {
		return "No characters found"
	}

	// Convert map to slice for sorting
	type charCount struct {
		char  rune
		count int
	}

	var charCounts []charCount
	for char, count := range occurrences {
		charCounts = append(charCounts, charCount{char, count})
	}

	// Sort alphabetically by character
	sort.Slice(charCounts, func(i, j int) bool {
		return charCounts[i].char < charCounts[j].char
	})

	// Build output string
	var result strings.Builder
	for i, cc := range charCounts {
		if i > 0 {
			result.WriteString(", ")
		}
		result.WriteString(fmt.Sprintf("'%c':%d", cc.char, cc.count))
	}

	return result.String()
}

func main() {
	if len(os.Args) != 2 {
		fmt.Println("Usage: go run char_counter.go '<input_text>'")
		os.Exit(1)
	}

	input := os.Args[1]
	occurrences := countCharOccurrences(input)
	output := buildCharCountOutput(occurrences)
	fmt.Printf("Character Count: %s\n", output)
}
