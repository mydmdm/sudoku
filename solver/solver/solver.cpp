#include "solver.h"

using namespace std;

static const int n = 9;
static const int n2 = n * n;
static const int sqrtn = sqrt(n);

static const int possible = 1;
static const int impossible = 0;

inline bool islast(int i, int a) {
    return (i%a) == (a - 1);
}

class node {
public:
    node() : _value(0) {};
    ~node() {};
    void set(int s) {
        if (s >= 1 && s <= 9) {
            _value = s;
        } else {
            _value = 0;
            _likely.resize(n, possible);
        }
    }

    std::vector<int> _likely;
    int _value;
};

class xy {
public:
    xy(int idx_in) : _idx(idx_in), _x(x()), _y(y()) {
    }
    xy(int x_in, int y_in) : _x(x_in), _y(y_in), _idx(idx()) {
    }
    int idx() {
        return _y * n2 + _x;
    }
    int x() {
        return _idx % n;
    }
    int y() {
        return _idx / n;
    }
    int _x, _y;
    int _idx;
};

class solver {
public:
    solver(const char *line) {
        _nodes.resize(n2);
        for (auto k = 0; k != n2; ++k) {
            _nodes[k].set(line[k] - '0');
        }
    };
    ~solver() {};
    void display(FILE *fp) {
        for (auto k = 0; k != n2; ++k) {
            if (k % (n*sqrtn) == 0) {
                for (auto i = 0; i != n + sqrtn; ++i) { fprintf(fp, "-"); }
                fprintf(fp, "\n");
            }
            _nodes[k]._value != 0 ? fprintf(fp, "%d", _nodes[k]._value) : fprintf(fp, " ") ;
            islast(k, sqrtn) ? fprintf(fp, "|") : 0;
            islast(k, n) ? fprintf(fp, "\n") : 0;
        }
        for (auto i = 0; i != n + sqrtn; ++i) { fprintf(fp, "-"); }
        fprintf(fp, "\n");
    }
    //
    std::vector<node> _nodes;
    std::stack<int> _event;
};

int main() {
    const char *puzzle = "4.....8.5.3..........7......2.....6.....8.4......1.......6.3.7.5..2.....1.4......";
    solver slv(puzzle);
    slv.display(stdout);

    fgetc(stdin);
    return 0;
}
