using System;
using System.Collections;
using System.Collections.Generic;

namespace Fahrenheit.Modules.Debug;

public static class StackExt {
    public static T PopOr<T>(this Stack<T> stack, Exception on_error) {
        return stack.TryPop(out T? res) ? res : throw on_error;
    }

    public static T PopOrDefault<T>(this Stack<T> stack, T def) {
        return stack.TryPop(out T? res) ? res : def;
    }

    public static T PeekOr<T>(this Stack<T> stack, Exception on_error) {
        return stack.TryPeek(out T? res) ? res : throw on_error;
    }

    public static T PeekOrDefault<T>(this Stack<T> stack, T def) {
        return stack.TryPeek(out T? res) ? res : def;
    }

    public static T? TryPop<T>(this Stack<T> stack) where T: class {
        return stack.TryPop(out T? res) ? res : null;
    }

    public static T? TryPeek<T>(this Stack<T> stack) where T: class {
        return stack.TryPeek(out T? res) ? res : null;
    }
}

public static class ICollectionExt {
    public static bool IsEmpty(this ICollection collection) {
        return collection.Count <= 0;
    }
}

public static class ArrayExt {
    public static bool IsEmpty<T>(this T[] array) {
        return array.Length <= 0;
    }
}