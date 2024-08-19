from internals.area import Area, scale_area
from internals.phenotype import Phenotype
from internals.graph_genome.room import Room
from internals.graph_genome import mutation, crossover, generation, constants
from internals.graph_genome.constants import GG_NUM_COLUMNS, GG_NUM_ROWS, GG_MAX_ROOM_WIDTH, GG_MAX_ROOM_HEIGHT, \
    GG_SQUARE_SIZE, GG_MAP_SCALE


class GraphGenome:
    def __init__(self, rooms, vertical_corridors, horizontal_corridors, map_scale):
        self.mapScale = map_scale
        self.rooms = rooms
        self.verticalCorridors = vertical_corridors
        self.horizontalCorridors = horizontal_corridors

        self.squareSize = GG_SQUARE_SIZE
        self.cellsWidth = GG_MAX_ROOM_WIDTH
        self.cellsHeight = GG_MAX_ROOM_HEIGHT
        self.numColumns = GG_NUM_COLUMNS
        self.numRows = GG_NUM_ROWS

    def __eq__(self, other):
        if type(other) is type(self):
            return self.__dict__ == other.__dict__
        else:
            return False
        
    # --- Generation methods ---

    @staticmethod
    def create_random_genome():
        return generation.create_random_genome()
    
    # --- Genetic operators ---

    @staticmethod
    def mutate(genome):
        return mutation.mutate(genome)
    
    @staticmethod
    def crossover(genome1, genome2):
        return crossover.crossover(genome1, genome2)

    # --- Conversion methods ---

    def to_array(self):
        array = []
        for row in self.rooms:
            for room in row:
                if room is None:
                    array.append(0)
                    array.append(0)
                    array.append(0)
                    array.append(0)
                else:
                    array.append(room.leftColumn)
                    array.append(room.rightColumn)
                    array.append(room.bottomRow)
                    array.append(room.topRow)
        for row in self.verticalCorridors:
            for corridor in row:
                array.append(1 if corridor else 0)
        for row in self.horizontalCorridors:
            for corridor in row:
                array.append(1 if corridor else 0)
        return array
    
    @staticmethod
    def array_as_genome(array):
        rooms = []
        vertical_corridors = []
        horizontal_corridors = []
        index = 0
        for _ in range(GG_NUM_ROWS):
            rooms.append([])
            for _ in range(GG_NUM_COLUMNS):
                left_col = array[index]
                index += 1
                right_col = array[index]
                index += 1
                bottom_row = array[index]
                index += 1
                top_row = array[index]
                index += 1
                if left_col == 0 and right_col == 0 and bottom_row == 0 and top_row == 0:
                    rooms[-1].append(None)
                else:
                    rooms[-1].append(Room(left_col, right_col, bottom_row, top_row))
        for _ in range(GG_NUM_ROWS - 1):
            vertical_corridors.append([])
            for _ in range(GG_NUM_COLUMNS):
                vertical_corridors[-1].append(array[index] == 1)
                index += 1
        for _ in range(GG_NUM_ROWS):
            horizontal_corridors.append([])
            for _ in range(GG_NUM_COLUMNS - 1):
                horizontal_corridors[-1].append(array[index] == 1)
                index += 1
        return GraphGenome(rooms, vertical_corridors, horizontal_corridors, GG_MAP_SCALE)

    def phenotype(self):
        areas = []

        # Resize rooms: If I have two adjacent rooms that should not see each other, I need to resize one in
        # order to prevent them from touching. This is needed because thin walls are not supported in the char map.
        # At the same time, if I have two adjacent rooms but they don't really touch each other, I need to separate
        # them just a little.
        for c in range(self.numColumns):
            for r in range(self.numRows - 1, 0, -1):
                self.shrink_vertically_if_needed(r, c)

        for r in range(self.numRows):
            for c in range(self.numColumns - 1, 0, -1):
                self.shrink_horizontally_if_needed(r, c)

        visited_rooms = self.find_closest_connected_component()

        # Start placing the Areas for Rooms
        for r in range(self.numRows):
            for c in range(self.numColumns):
                room = self.rooms[r][c]
                if room is None:
                    continue
                if not visited_rooms[r][c]:
                    continue
                area = Area(
                    c * self.cellsWidth + room.leftColumn,
                    r * self.cellsHeight + room.bottomRow,
                    c * self.cellsWidth + room.rightColumn,
                    r * self.cellsHeight + room.topRow,
                )
                areas.append(area)

        # Place corridors
        for r in range(1, self.numRows):
            for c in range(self.numColumns):
                if not visited_rooms[r][c]:
                    continue
                self.place_vertical_connections_if_needed(areas, r, c)

        for c in range(1, self.numColumns):
            for r in range(self.numRows):
                if not visited_rooms[r][c]:
                    continue
                self.place_horizontal_connections_if_needed(areas, r, c)

        if any(area.bottomRow == area.topRow or area.leftColumn == area.rightColumn for area in areas):
            print("There is an invalid area!")

        if len(areas) == 0:
            return None

        return Phenotype(
            self.cellsWidth * self.numColumns * self.squareSize,
            self.cellsHeight * self.numRows * self.squareSize,
            self.mapScale,
            [scale_area(area, self.squareSize) for area in areas],
        )

    # --- Utils ---

    @staticmethod
    def unscaled_area(phenotype):
        return sum([
            (x.rightColumn - x.leftColumn) * (x.topRow - x.bottomRow) for x in phenotype.areas
        ]
        )

    def find_closest_connected_component(self):
        rows = len(self.rooms)
        columns = len(self.rooms[0])

        visited_rooms = [[False for _ in range(columns)] for _ in range(rows)]

        closest_row = 0
        closest_column = 0
        closest_distance = float("inf")

        center_row = (rows - 1) / 2
        center_col = (columns - 1) / 2

        for r in range(rows):
            for c in range(columns):
                if self.rooms[r][c] is None:
                    continue

                room_score = abs(center_row - r) + abs(center_col - c)

                if room_score > closest_distance:
                    continue

                closest_column = c
                closest_row = r
                closest_distance = int(room_score)

        self.visit_tree(closest_row, closest_column, visited_rooms)

        return visited_rooms

    def visit_tree(self, r, c, visited):
        if visited[r][c]:
            # cell already visited.
            return

        if self.rooms[r][c] is None:
            return
        visited[r][c] = True

        if r > 0 and self.verticalCorridors[r - 1][c]:
            self.visit_tree(r - 1, c, visited)

        if r < len(self.rooms) - 1 and self.verticalCorridors[r][c]:
            self.visit_tree(r + 1, c, visited)

        if c > 0 and self.horizontalCorridors[r][c - 1]:
            self.visit_tree(r, c - 1, visited)

        if c < len(self.rooms[0]) - 1 and self.horizontalCorridors[r][c]:
            self.visit_tree(r, c + 1, visited)

    def place_vertical_connections_if_needed(self, areas, r, c):
        top_room = self.rooms[r][c]
        bottom_room = self.rooms[r - 1][c]

        if top_room is None or bottom_room is None:
            # Rooms are not real
            return

        if not self.verticalCorridors[r - 1][c]:
            # Rooms are not connected, nothing to do.
            return

        spaced_vertically = bottom_room.topRow != GG_MAX_ROOM_HEIGHT or top_room.bottomRow != 0
        intersect_horizontally = bottom_room.leftColumn < top_room.rightColumn and \
                                 top_room.leftColumn < bottom_room.rightColumn

        if not spaced_vertically and intersect_horizontally:
            # Rooms are touching, nothing to do
            return

        top_y = r * GG_MAX_ROOM_HEIGHT + top_room.bottomRow
        bottom_y = (r - 1) * GG_MAX_ROOM_HEIGHT + bottom_room.topRow

        max_of_min_x = max(bottom_room.leftColumn, top_room.leftColumn)
        min_of_max_x = min(bottom_room.rightColumn, top_room.rightColumn)

        if max_of_min_x < min_of_max_x:
            # We can place a straight corridor.
            # start_x = c * cells_width + random.randrange(max_of_min_x, min_of_max_x)
            start_x = c * GG_MAX_ROOM_WIDTH + int((max_of_min_x + min_of_max_x) // 2)
            areas.append(Area(
                start_x,
                bottom_y,
                start_x + 1,
                top_y,
                True))
            return

        # We cannot place a straight corridor. Place an twisted one.
        # Find X1 and X2 of the corridor.
        middle_x_top_room = c * GG_MAX_ROOM_WIDTH + (top_room.leftColumn + top_room.rightColumn) // 2
        middle_x_bottom_room = c * GG_MAX_ROOM_WIDTH + (bottom_room.leftColumn + bottom_room.rightColumn) // 2

        # var corridorY = Random.Range(bottomY, topY);
        corridor_y = (bottom_y + top_y) // 2

        if bottom_y != corridor_y:
            # Vertical corridor from bottom room
            areas.append(Area(middle_x_bottom_room, bottom_y, middle_x_bottom_room + 1, corridor_y, True))

        if top_y != corridor_y:
            # Vertical corridor from top room
            areas.append(Area(middle_x_top_room, corridor_y, middle_x_top_room + 1, top_y, True))

        starting_x = min(middle_x_bottom_room, middle_x_top_room)
        ending_x = max(middle_x_bottom_room, middle_x_top_room) + 1
        # Horizontal corridor
        areas.append(Area(starting_x, corridor_y, ending_x, corridor_y + 1, True))

    def place_horizontal_connections_if_needed(self, areas, r, c):
        left_room = self.rooms[r][c - 1]
        right_room = self.rooms[r][c]

        if right_room is None or left_room is None:
            # Rooms are not real
            return

        if not self.horizontalCorridors[r][c - 1]:
            # Rooms are not connected, nothing to do.
            return

        spaced_horizontally = left_room.rightColumn != GG_MAX_ROOM_WIDTH or right_room.leftColumn != 0
        intersect_vertically = left_room.bottomRow < right_room.topRow and right_room.bottomRow < left_room.topRow

        if not spaced_horizontally and intersect_vertically:
            # Rooms are touching, nothing to do
            return

        right_x = c * GG_MAX_ROOM_WIDTH + right_room.leftColumn
        left_x = (c - 1) * GG_MAX_ROOM_WIDTH + left_room.rightColumn

        max_of_min_y = max(left_room.bottomRow, right_room.bottomRow)
        min_of_max_y = min(left_room.topRow, right_room.topRow)

        if max_of_min_y < min_of_max_y:
            # We can place a straight corridor.
            start_y = r * GG_MAX_ROOM_HEIGHT + (max_of_min_y + min_of_max_y) // 2
            areas.append(Area(left_x, start_y, right_x, start_y + 1, True))
            return

        # We cannot place a straight corridor. Place an twisted one.
        # Find Y1 and Y2 of the corridor.
        middle_y_right_room = r * GG_MAX_ROOM_HEIGHT + (right_room.bottomRow + right_room.topRow) // 2
        middle_y_left_room = r * GG_MAX_ROOM_HEIGHT + (left_room.bottomRow + left_room.topRow) // 2

        corridor_x = (left_x + right_x) // 2
        # Vertical corridor from bottom room
        if left_x != corridor_x:
            areas.append(Area(left_x, middle_y_left_room, corridor_x, middle_y_left_room + 1, True))

        if corridor_x != right_x:
            # Vertical corridor from top room
            areas.append(Area(corridor_x, middle_y_right_room, right_x, middle_y_right_room + 1, True))

        starting_y = min(middle_y_left_room, middle_y_right_room)
        ending_y = max(middle_y_left_room, middle_y_right_room) + 1
        # Horizontal corridor
        areas.append(Area(corridor_x, starting_y, corridor_x + 1, ending_y, True))

    def shrink_vertically_if_needed(self, r, c):
        bottom_room = self.rooms[r - 1][c]
        top_room = self.rooms[r][c]
        if bottom_room is None or top_room is None:
            # A Room is not real
            return

        spaced_vertically = bottom_room.topRow != GG_MAX_ROOM_HEIGHT or top_room.bottomRow != 0
        if spaced_vertically:
            # There is space for a corridor or to keep the rooms separated.
            return

        # TODO Check
        intersect_horizontally = bottom_room.leftColumn < top_room.rightColumn and \
                                 top_room.leftColumn < bottom_room.rightColumn

        if self.verticalCorridors[r - 1][c] and intersect_horizontally:
            # The rooms touch each other, so there is no need to make space for a corridor.
            return

        if not self.verticalCorridors[r - 1][c] and not intersect_horizontally:
            # The rooms are separated and do not need a corridor.
            return

        # We need space, either to separate the two rooms or to fit a corridor in the middle.
        old_room = bottom_room
        new_room = Room(
            old_room.leftColumn,
            old_room.rightColumn,
            max(0, old_room.bottomRow - 1),
            old_room.topRow - 1,
        )
        self.rooms[r - 1][c] = new_room

    def shrink_horizontally_if_needed(self, r, c):
        left_room = self.rooms[r][c - 1]
        right_room = self.rooms[r][c]

        if left_room is None or right_room is None:
            # A Room is not real
            return

        spaced_horizontally = left_room.rightColumn != GG_MAX_ROOM_WIDTH or right_room.leftColumn != 0
        if spaced_horizontally:
            # There is space for a corridor or to keep the rooms separated.
            return

        # TODO Check
        intersect_vertically = left_room.bottomRow < right_room.topRow and right_room.bottomRow < left_room.topRow

        if self.horizontalCorridors[r][c - 1] and intersect_vertically:
            # The rooms touch each other, so there is no need to make space for a corridor.
            return

        if not self.horizontalCorridors[r][c - 1] and not intersect_vertically:
            # The rooms are separated and do not need a corridor.
            return

        # We need space, either to separate the two rooms or to fit a corridor in the middle.
        old_room = left_room
        self.rooms[r][c - 1] = Room(
            max(0, old_room.leftColumn - 1),
            old_room.rightColumn - 1,
            old_room.bottomRow,
            old_room.topRow,
        )
