

import urllib.request
import requests
import cv2
import matplotlib.pyplot as plt
import numpy as np
import itertools
import json
import sys

def getApproximateConvexArea(contour):
    appx = cv2.approxPolyDP(contour, 4, True)
    if len(appx)!=4 or not cv2.isContourConvex(appx):
        return 0
    return cv2.contourArea(appx)

def getContourImage(img, contour, target=None):
    x,y,w,h = cv2.boundingRect(contour)
    if target == None:
        w1, h1 = w, h
    elif w <= target[1] and h <= target[0]:
        w1, h1 = target[1], target[0]
    elif h/w <= target[1]/target[0]:
        w1, h1 = w, int(target[1]*w/target[0])
    else:
        w1, h1 = int(h*target[0]/target[1]), h
    x1, y1 = x + (w - w1)//2, y + (h - h1)//2
    rst = img[y1:y1+h1, x1:x1+w1]
    if w != w1 or h != h1:
        rst = cv2.resize(rst, target)
    return rst, [x1,y1,w1,h1]

def getBiggestConvexContour(img):
    _, contours, hierarchy = cv2.findContours(img,cv2.RETR_TREE,cv2.CHAIN_APPROX_SIMPLE)
    area = [getApproximateConvexArea(c) for c in contours]
    k = np.argmax(np.array(area))
    return contours[k]

def segmentPuzzle(img):
    # smooth the picture (low-pass filtered)
    img_gray = cv2.blur(img, (3,3))
    
    # get the biggest convex contour and crop it out
    thr = cv2.adaptiveThreshold(img,255,1,1,15,15)    
    contour = getBiggestConvexContour(thr)
    img_puzzle, pos_puzzle = getContourImage(img, contour)
    return img_puzzle, pos_puzzle


def getDigitImages(img):
    y = [int(k*img.shape[0]/9) for k in range(10)]
    x = [int(k*img.shape[1]/9) for k in range(10)]
    out = [[img[y[i]:y[i+1],x[j]:x[j+1]] for j in range(9)] for i in range(9)]
    return out


def contourCloseToBoundary(contour, img, thr=0.1):
    ymax, xmax = img.shape
    x,y,w,h = cv2.boundingRect(contour)
    rx = (x if x*2 + w <= xmax else xmax - (x+w)) / xmax
    ry = (y if y*2 + h <= ymax else ymax - (y+h)) / ymax
    return min(rx,ry) <= thr
       
def contourIsWholeImg(contour, img):
    ymax, xmax = img.shape
    x,y,w,h = cv2.boundingRect(contour)
    return x==0 and y==0 and w==xmax and h==ymax
    
def rescaleImage(img, shape=(28,28)):
    edges = np.copy(img)
    edges = cv2.adaptiveThreshold(edges,255,cv2.ADAPTIVE_THRESH_MEAN_C,cv2.THRESH_BINARY,15,15)
    _, contours, hierarchy = cv2.findContours(edges,cv2.RETR_TREE,cv2.CHAIN_APPROX_SIMPLE)
    validCont = []
    for k in list(range(len(contours))):
        if contourIsWholeImg(contours[k],img) or contourCloseToBoundary(contours[k], img, thr=0.2):
            continue
        validCont.append(contours[k])
    if len(validCont) == 0:
        return True, np.zeros(shape)
    hull = cv2.convexHull(np.vstack(validCont))
    a = cv2.contourArea(hull) / (img.shape[0] * img.shape[1])
    if a < 0.01:
        return True, np.zeros(shape)
    rst, _ = getContourImage(img, hull, shape)
    return False, rst


'''
azCogSrvCfg = {
    "domain": "recognizeText",
    "region": "eastasia",
    "headers":{
        'Content-Type': "application/octet-stream",
        'Ocp-Apim-Subscription-Key': "",        
    },
    "params":{
        'handwriting': 'false',
    }
}
'''

    
def requestWrapper(method, url, params, maxNumRetries=10, retryCode=[429], exitCode=[200]):
    retries = 0
    while True:
        response = requests.request(method, url, **params)
        if response.status_code in exitCode:
            return response
        if response.status_code in retryCode:
            print( "Message: %s" % ( response.json() ) )
            if retries <= _maxNumRetries: 
                time.sleep(1) 
                retries += 1
                continue
            else: 
                print( 'Error: failed after retrying!' )
                break
        else:
            print( "Error code: %d" % ( response.status_code ) )
            print( "Message: %s" % ( response.json() ) )
        break        
    return None
        

def azCognitiveServiceRecognizeText(cfg, data):
    url = 'https://%s.api.cognitive.microsoft.com/vision/v1.0/%s' % (cfg['region'], cfg['domain'])

    request_dict = dict()
    request_dict['headers'] = cfg['headers']
    request_dict['params'] = cfg['params']
    
    if cfg['headers']['Content-Type'] == 'application/json':
        request_dict['json'] = {'url':data}
    elif cfg['headers']['Content-Type'] == "application/octet-stream":
        request_dict['data'] = data

    response, result = None, None
    response = requestWrapper('post', url, request_dict, exitCode=[202,200])
    if response != None and response.status_code == 202:
        response = requestWrapper('get', response.headers['Operation-Location'], {'headers':cfg['headers']})
    if response:
        result = response.json()
    return result
        
def solveForward(puzzle, n=9):
    values = np.copy(puzzle).astype('int')
    newInfo = True
    while newInfo:
        newInfo = False
        minFreedom, position, candidates = n, (-1,-1), []
        try:
            for idx in itertools.product(range(n), repeat=2):
                x,y = idx[0], idx[1]
                if values[x][y] > 0:
                    continue
                digit_found = np.zeros(n+1)
                digit_found[values[x,:]] = 1 # column 
                digit_found[values[:,y]] = 1 # row
                cell_x, cell_y = [(x//3)*3 + (i//3) for i in range(n)], [(y//3)*3 + (i%3) for i in range(n)]
                digit_found[values[cell_x, cell_y]] = 1 # cell
                digit_found = digit_found[1:] # remove uncertainty (zero)
                u = np.count_nonzero(digit_found == 0)
                v = np.argwhere(digit_found == 0) + 1
                if u == 0:
                    raise
                elif u == 1:
                    values[x][y] = v[0][0]
                    newInfo = True
                elif u <= minFreedom:
                    minFreedom, position, candidates = u, (x,y), v
        except:
            return 'failed', values
    status = 'solved' if np.count_nonzero(values == 0) == 0 else 'unfinished'
    if status == 'unfinished': # unfinished but not failed
        for v in candidates:
            puzzle_new = np.copy(values)
            puzzle_new[position] = v[0]
            status_new, solution_new = solveForward(puzzle_new)
            if status_new == 'solved':
                return status_new, solution_new
    return status, values


def getDigitPosition(pos, leftBottom, textSize):
    x, y, w, h = leftBottom
    return (x + int(w*(pos[1]+0.5)/9) - (textSize[1]//2), y + int(h*(pos[0]+0.5)/9) + (textSize[0]//2))


def showImage(img, lib='cv2', win='image'):
    if lib == 'cv2':
        cv2.imshow(win, img)
        cv2.waitKey()
    elif lib == 'plt':
        pass

def processOnePicture(filename):
    print(filename)
    img = cv2.imread(filename)

    # BGR format to gray
    img_gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    img_gray = cv2.blur(img_gray, (3,3))
    img_puzzle, pos_puzzle = segmentPuzzle(img_gray)

    img_digits = getDigitImages(img_puzzle)

    shapeRecognize = (50,50)
    img_mdl = np.zeros((9,9,*shapeRecognize))
    flagEmpty = np.zeros((9,9))
    for i in itertools.product(range(9), repeat=2):
        flagEmpty[i], img_mdl[i] = rescaleImage(img_digits[i[0]][i[1]], shapeRecognize)
    
    cachefile = r'all_digits.jpg'
    img_all_digits = np.concatenate([img_mdl[i] for i in itertools.product(range(9), repeat=2) if not flagEmpty[i]], axis=1)
    cv2.imwrite(cachefile, img_all_digits)

    with open('cognitive_service.json', 'r') as f:
        azCogSrvCfg = json.load(f)

    with open(cachefile, 'rb') as f:
        data = f.read()
    result = azCognitiveServiceRecognizeText(azCogSrvCfg, data)
    if result:
        print(result)

    digits_str = result['regions'][0]['lines'][0]['words'][0]['text']
    puzzle = np.zeros((9,9), dtype='int')
    k = 0
    for i in itertools.product(range(9), repeat=2):
        if not flagEmpty[i]:
            puzzle[i] = int(digits_str[k])
            k = k + 1
    print(puzzle)

    status, solution = solveForward(puzzle)
    print(status)
    print(solution)

    solutionColor = (0,0,255)
    img_solution = np.copy(img)
    for i in itertools.product(range(9), repeat=2):
        if puzzle[i] != 0:
            continue
        fontName = cv2.FONT_HERSHEY_SIMPLEX
        fontScale = 1.5
        fontThick = 2
        fs = cv2.getTextSize(str(solution[i]), fontName, fontScale, fontThick)
        cv2.putText(img_solution, str(solution[i]), getDigitPosition(i,pos_puzzle,fs[0]), fontName, fontScale, solutionColor, fontThick)
    showImage(img_solution)
    _ = cv2.imwrite('solution.jpg', img_solution) 

    result = dict()
    result['image'] = img
    result['image_solution'] = img_solution
    result['image_puzzle'] = img_puzzle
    result['image_digits'] = img_mdl
    result['puzzle'] = puzzle
    result['solution'] = solution

    return result

def createTestDataSet(cache):
    puzzle, img = cache['puzzle'], cache['image_digits']
    x_test, y_test = [], []
    for i in itertools.product(range(9), repeat=2):
        if puzzle[i] > 0:
            y_test.append(puzzle[i])
            tmp = cv2.adaptiveThreshold(img[i],255,cv2.ADAPTIVE_THRESH_MEAN_C,cv2.THRESH_BINARY,15,15)
    x_test, y_test = np.array(x_test), np.array(y_test)
    np.savez('test', x_test, y_test)

if __name__ == "__main__":

    # url is what shown in the IP web camera application
    # url='http://211.192.192.98:8080/shot.jpg'
    url = sys.argv[1]
    cv2.namedWindow("image", cv2.WINDOW_NORMAL)

    if url.startswith('http'):
        pass
    else:
        processOnePicture(url)
        exit(0)


    while True:
        # Use urllib to get the image and convert into a cv2 usable format
        imgResp=urllib.request.urlopen(url)
        imgNp=np.array(bytearray(imgResp.read()),dtype=np.uint8)
        img=cv2.imdecode(imgNp,flags=cv2.IMREAD_GRAYSCALE )

        print(img.shape)
        while min(img.shape[0:2]) > 1000:
            img = cv2.resize(img, (0,0), fx=0.5, fy=0.5)

        # put the image on screen

        #To give the processor some less stress
        #time.sleep(0.1) 

        # extract digits
        edges = cv2.Canny(img, 30, 90)
        lines = cv2.HoughLines(edges, 2, np.pi /180, 300, 0, 0)

        _, contours, hierarchy = cv2.findContours(img,cv2.RETR_TREE,cv2.CHAIN_APPROX_SIMPLE)

        img = cv2.drawContours(img, contours, -1, (255,0,0))

        #cv2.imshow('Solver',img)
        #cv2.waitKey()
        plt.imshow(img, cmap='gray')
        plt.show()
        
        break
        if 0xFF == ord('q'):
            break


