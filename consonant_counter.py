import sys

def count_consonants(input_text):
    """Count the number of consonants in the input text."""
    consonants = 'bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ'
    return sum(1 for char in input_text if char in consonants)

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python consonant_counter.py '<input_text>'")
        sys.exit(1)

    input_text = sys.argv[1]
    output = count_consonants(input_text)
    print(f"Consonant Count: {output}") 